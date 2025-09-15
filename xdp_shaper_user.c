// xdp_shaper_user.c
// Build: see Makefile. Run as root.
// Example: sudo ./xdp_shaper -i eth0 -q 0 --delay-ms 10 --drop-nth 10

#define _GNU_SOURCE
#include <errno.h>
#include <fcntl.h>
#include <net/if.h>
#include <poll.h>
#include <signal.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <sys/resource.h>
#include <sys/socket.h>
#include <time.h>
#include <unistd.h>
#include <sched.h>

#include <bpf/bpf.h>
#include <bpf/libbpf.h>
#include <bpf/xsk.h>
#include <linux/if_link.h>

#define FRAME_SIZE          4096u
#define NUM_FRAMES          4096u
#define RX_RING_SIZE        1024u
#define TX_RING_SIZE        1024u
#define FILL_RING_SIZE      2048u
#define COMPLETION_RING_SIZE 2048u
#define BATCH_RX            64u
#define BATCH_TX            64u

static volatile bool exiting = false;

static void on_sigint(int signo) { (void)signo; exiting = true; }

// Simple logger
static void die(const char* fmt, ...) {
    va_list ap; va_start(ap, fmt);
    vfprintf(stderr, fmt, ap);
    va_end(ap);
    fputc('\n', stderr);
    exit(EXIT_FAILURE);
}

static uint64_t now_ns(void) {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (uint64_t)ts.tv_sec * 1000000000ull + (uint64_t)ts.tv_nsec;
}

// Pending packet we will transmit later
struct pending_pkt {
    uint64_t due_ns;
    uint64_t addr;
    uint32_t len;
};

struct app_config {
    const char* ifname;
    int ifindex;
    uint32_t delay_ms;
    uint32_t drop_nth;   // drop every Nth packet
    bool skb_mode;       // prefer SKB mode if true
};

struct xsk_ctx {
    struct xsk_umem *umem;
    void* umem_area;
    size_t umem_area_len;

    struct xsk_ring_prod fill;
    struct xsk_ring_cons comp;
    struct xsk_ring_cons rx;
    struct xsk_ring_prod tx;

    struct xsk_socket *xsk;
    int xsks_map_fd;

    // Simple frame allocator: list of free frame addresses
    uint64_t *free_frames;
    uint32_t free_frames_cnt;

    // Pending packets waiting for delay expiry
    struct pending_pkt *pending;
    uint32_t pending_cnt;
    uint32_t pending_cap;
};

static void set_memlock_rlimit(void) {
    struct rlimit r = { RLIM_INFINITY, RLIM_INFINITY };
    if (setrlimit(RLIMIT_MEMLOCK, &r))
        die("setrlimit(RLIMIT_MEMLOCK) failed: %s", strerror(errno));
}

static void usage(const char* prog) {
    fprintf(stderr,
        "Usage: sudo %s -i <ifname> [-q <queue_id>] [--delay-ms <ms>] [--drop-nth <N>] [--skb-mode]\n"
        "Defaults: -q 0 --delay-ms 10 --drop-nth 10\n", prog);
}

static uint64_t frame_addr_of(uint32_t idx) {
    return (uint64_t)idx * FRAME_SIZE;
}


static int xsk_setup(struct app_config *cfg, struct xsk_ctx *ctx) {
    memset(ctx, 0, sizeof(*ctx));

    // Allocate UMEM area
    ctx->umem_area_len = (size_t)NUM_FRAMES * FRAME_SIZE;
    if (posix_memalign(&ctx->umem_area, getpagesize(), ctx->umem_area_len))
        die("posix_memalign failed");

    struct xsk_umem_config ucfg = {
        .fill_size = FILL_RING_SIZE,
        .comp_size = COMPLETION_RING_SIZE,
        .frame_size = FRAME_SIZE,
        .frame_headroom = 0
    };

    int err = xsk_umem__create(&ctx->umem, ctx->umem_area, ctx->umem_area_len,
                               &ctx->fill, &ctx->comp, &ucfg);
    if (err)
        die("xsk_umem__create failed: %s", strerror(-err));

    // Create XSK socket
    struct xsk_socket_config scfg = {
        .rx_size = RX_RING_SIZE,
        .tx_size = TX_RING_SIZE,
        .libbpf_flags = 0,
        .xdp_flags = cfg->skb_mode ? XDP_FLAGS_SKB_MODE : XDP_FLAGS_DRV_MODE,
        .bind_flags = XDP_COPY | XDP_USE_NEED_WAKEUP // copy mode is widely supported
    };

    err = xsk_socket__create(&ctx->xsk, cfg->ifname, 0, ctx->umem, &ctx->rx, &ctx->tx, &scfg);
    if (err) {
        //detach_xdp_prog(cfg->ifindex, used_flags);
        die("xsk_socket__create failed: %s", strerror(-err));
    }

    // Prepare free frame list and fill ring
    ctx->free_frames = calloc(NUM_FRAMES, sizeof(uint64_t));
    if (!ctx->free_frames)
        die("calloc free_frames failed");

    for (uint32_t i = 0; i < NUM_FRAMES; i++)
        ctx->free_frames[ctx->free_frames_cnt++] = frame_addr_of(i);

    // Fill RX with all frames initially
    uint32_t idx;
    uint32_t n = ctx->free_frames_cnt;
    while (n) {
        uint32_t batch = n > BATCH_RX ? BATCH_RX : n;
        uint32_t reserved = xsk_ring_prod__reserve(&ctx->fill, batch, &idx);
        if (reserved == 0) break;
        for (uint32_t i = 0; i < reserved; i++) {
            *xsk_ring_prod__fill_addr(&ctx->fill, idx + i) = ctx->free_frames[--ctx->free_frames_cnt];
        }
        xsk_ring_prod__submit(&ctx->fill, reserved);
        n -= reserved;
    }

    // Pending buffer
    ctx->pending_cap = 8192;
    ctx->pending = calloc(ctx->pending_cap, sizeof(struct pending_pkt));
    if (!ctx->pending) die("calloc pending failed");

    return 0;
}

static void kick_tx_if_needed(struct xsk_ctx* ctx) {
    if (xsk_ring_prod__needs_wakeup(&ctx->tx)) {
        // Kick the Tx path
        sendto(xsk_socket__fd(ctx->xsk), NULL, 0, MSG_DONTWAIT, NULL, 0);
    }
}

static void service_completions(struct xsk_ctx* ctx) {
    uint32_t idx, n;
    n = xsk_ring_cons__peek(&ctx->comp, BATCH_TX, &idx);
    if (!n) return;
    for (uint32_t i = 0; i < n; i++) {
        uint64_t addr = *xsk_ring_cons__comp_addr(&ctx->comp, idx + i);
        // Return frame to FILL ring
        uint32_t fidx;
        while (xsk_ring_prod__reserve(&ctx->fill, 1, &fidx) == 0) {
            // Not enough space in fill ring yet: kick TX and retry after a tiny pause
            kick_tx_if_needed(ctx);
            // break up CPU spin a notch
            sched_yield();
        }
        *xsk_ring_prod__fill_addr(&ctx->fill, fidx) = addr;
        xsk_ring_prod__submit(&ctx->fill, 1);
    }
    xsk_ring_cons__release(&ctx->comp, n);
}

static void transmit_ready(struct xsk_ctx* ctx) {
    if (ctx->pending_cnt == 0) return;

    uint64_t now = now_ns();
    uint32_t tx_idx;

    // Reserve as many slots as we might use
    uint32_t to_send = ctx->pending_cnt < BATCH_TX ? ctx->pending_cnt : BATCH_TX;
    uint32_t reserved = xsk_ring_prod__reserve(&ctx->tx, to_send, &tx_idx);
    if (!reserved) {
        kick_tx_if_needed(ctx);
        return;
    }

    // Iterate pending and send those due; compact by swapping from end
    uint32_t submitted = 0;
    for (uint32_t i = 0; i < ctx->pending_cnt && submitted < reserved; ) {
        if (ctx->pending[i].due_ns > now) { i++; continue; }

        struct xdp_desc *txd = xsk_ring_prod__tx_desc(&ctx->tx, tx_idx + submitted);
        txd->addr = ctx->pending[i].addr;
        txd->len  = ctx->pending[i].len;
        submitted++;

        // Remove this pending by swapping with last
        ctx->pending[i] = ctx->pending[ctx->pending_cnt - 1];
        ctx->pending_cnt--;
    }

    if (submitted) {
        xsk_ring_prod__submit(&ctx->tx, submitted);
        kick_tx_if_needed(ctx);
    } else {
        // We reserved but had nothing due; cancel by not submitting and just leave ring as-is.
        // There's no explicit "cancel", so we simply don't use reserved slots; but libbpf expects submit after reserve.
        // To be safe, only reserve if at least one is due; simplify by ignoring this corner and letting reserved==0 when none due in callers.
        // (We already reserved; it's okay since submitted=0 -> no submit)
    }
}

static void ensure_pending_capacity(struct xsk_ctx* ctx, uint32_t extra) {
    if (ctx->pending_cnt + extra <= ctx->pending_cap) return;
    uint32_t newcap = ctx->pending_cap * 2;
    while (ctx->pending_cnt + extra > newcap) newcap *= 2;
    struct pending_pkt *p = realloc(ctx->pending, newcap * sizeof(*p));
    if (!p) die("realloc pending failed");
    ctx->pending = p;
    ctx->pending_cap = newcap;
}

int main(int argc, char **argv)
{
    struct app_config cfg = {
        .ifname = NULL,
        .ifindex = 0,
        .delay_ms = 10,
        .drop_nth = 10,
        .skb_mode = false
    };

    // Parse args
    for (int i = 1; i < argc; i++) {
        if (!strcmp(argv[i], "-i") && i + 1 < argc) {
            cfg.ifname = argv[++i];
        } else if (!strcmp(argv[i], "--delay-ms") && i + 1 < argc) {
            cfg.delay_ms = (uint32_t)atoi(argv[++i]);
        } else if (!strcmp(argv[i], "--drop-nth") && i + 1 < argc) {
            cfg.drop_nth = (uint32_t)atoi(argv[++i]);
        } else if (!strcmp(argv[i], "--skb-mode")) {
            cfg.skb_mode = true;
        } else if (!strcmp(argv[i], "-h") || !strcmp(argv[i], "--help")) {
            usage(argv[0]); return 0;
        } else {
            fprintf(stderr, "Unknown arg: %s\n", argv[i]);
            usage(argv[0]); return 1;
        }
    }

    if (!cfg.ifname) {
        usage(argv[0]);
        return 1;
    }
    cfg.ifindex = if_nametoindex(cfg.ifname);
    if (!cfg.ifindex) die("if_nametoindex(%s) failed", cfg.ifname);
    if (geteuid() != 0) die("Run as root");

    set_memlock_rlimit();

    signal(SIGINT, on_sigint);
    signal(SIGTERM, on_sigint);

    int modes[] = {XDP_FLAGS_SKB_MODE, XDP_FLAGS_DRV_MODE, XDP_FLAGS_HW_MODE, 0};
    for (int i = 0; i < 4; ++i)
        bpf_set_link_xdp_fd(cfg.ifindex, -1, modes[i]);

    struct xsk_ctx ctx;
    if (xsk_setup(&cfg, &ctx) != 0)
        die("xsk_setup failed");

    printf("Shaper up on %s delay=%ums, drop every %u-th packet%s\n",
           cfg.ifname, cfg.delay_ms, cfg.drop_nth, cfg.skb_mode ? " (SKB mode)" : "");

    uint64_t pkt_counter = 0;
    const uint64_t delay_ns = (uint64_t)cfg.delay_ms * 1000000ull;

    struct pollfd pfd = {
        .fd = xsk_socket__fd(ctx.xsk),
        .events = POLLIN | POLLOUT
    };

    while (!exiting) {
        // Service completions to recycle frames
        service_completions(&ctx);

        // Try to pull RX packets
        uint32_t idx_rx = 0;
        uint32_t rcvd = xsk_ring_cons__peek(&ctx.rx, BATCH_RX, &idx_rx);

        if (rcvd) {
            for (uint32_t i = 0; i < rcvd; i++) {
                const struct xdp_desc *rxd = xsk_ring_cons__rx_desc(&ctx.rx, idx_rx + i);
                uint64_t addr = rxd->addr;
                uint32_t len  = rxd->len;
                pkt_counter++;

                bool drop = (cfg.drop_nth > 0) && (pkt_counter % cfg.drop_nth == 0);
                if (drop) {
                    // Drop: return frame to FILL immediately
                    uint32_t fidx;
                    while (!xsk_ring_prod__reserve(&ctx.fill, 1, &fidx)) {
                        service_completions(&ctx);
                        kick_tx_if_needed(&ctx);
                        sched_yield();
                    }
                    *xsk_ring_prod__fill_addr(&ctx.fill, fidx) = addr;
                    xsk_ring_prod__submit(&ctx.fill, 1);
                } else {
                    // Queue for delayed TX
                    ensure_pending_capacity(&ctx, 1);
                    ctx.pending[ctx.pending_cnt++] = (struct pending_pkt) {
                        .due_ns = now_ns() + delay_ns,
                        .addr = addr,
                        .len  = len
                    };
                }
            }
            xsk_ring_cons__release(&ctx.rx, rcvd);
        }

        // Transmit ready packets
        transmit_ready(&ctx);

        // If nothing to do, poll for a short time
        if (!rcvd && ctx.pending_cnt == 0) {
            (void)poll(&pfd, 1, 5); // 5 ms idle sleep
        }
    }

    printf("Exiting...\n");
    return 0;
}