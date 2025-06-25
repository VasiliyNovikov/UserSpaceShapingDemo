// gcc -O2 -Wall shaper_user.c -o shaper_user -lbpf -lelf -pthread
#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <net/if.h>          /* if_nametoindex            */
#include <poll.h>            /* poll(), POLLIN            */
#include <sys/socket.h>      /* sendto()                  */

#include <sys/mman.h>          /* mmap / PROT_* / MAP_* */
#include <time.h>              /* clock_gettime */

#include <bpf/libbpf.h>        /* libbpf API */
#include <bpf/bpf.h>           /* bpf_map_update_elem */
#include <linux/if_xdp.h>      /* struct xdp_options, bind flags      */
#include <linux/if_link.h>     /* XDP_FLAGS_SKB_MODE / _DRV / _HW     */
#include <bpf/xsk.h>           /* xsk_ring_* helpers */

#include "meta.h"

#define NUM_FRAMES  4096
#define FRAME_SIZE  XSK_UMEM__DEFAULT_FRAME_SIZE
#define UMEM_SIZE   (NUM_FRAMES * FRAME_SIZE)

static struct xsk_umem *umem;
static struct xsk_ring_prod fq;
static struct xsk_ring_cons cq;
static struct xsk_socket *xsk;
static struct xsk_ring_cons rx;
static struct xsk_ring_prod tx;
static void *umem_base;

static void setup_umem(void)
{
    umem_base = mmap(NULL, UMEM_SIZE,
                     PROT_READ | PROT_WRITE,
                     MAP_PRIVATE | MAP_ANONYMOUS | MAP_POPULATE, -1, 0);
    struct xsk_umem_config uc = {
        .fill_size = NUM_FRAMES,
        .comp_size = NUM_FRAMES,
        .frame_size = FRAME_SIZE,
        .frame_headroom = 0
    };
    xsk_umem__create(&umem, umem_base, UMEM_SIZE, &fq, &cq, &uc);

    /* pre-fill FQ with buffers */
    for (uint32_t i = 0; i < NUM_FRAMES; i++) {
        uint32_t idx;
        while (!xsk_ring_prod__reserve(&fq, 1, &idx));
        *xsk_ring_prod__fill_addr(&fq, idx) = i * FRAME_SIZE;
        xsk_ring_prod__submit(&fq, 1);
    }
}

/*
static int64_t now_us(void)
{
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return ts.tv_sec * 1000000LL + ts.tv_nsec / 1000;
}
*/

static void shape_ingress(uint8_t *pkt, uint32_t len)
{
    /* TODO: latency / drop algorithm for ingress */
    (void)pkt; (void)len;
}

static void shape_egress(uint8_t *pkt, uint32_t len)
{
    /* TODO: latency / drop algorithm for egress */
    (void)pkt; (void)len;
}

int main(int argc, char **argv)
{
    const char *iface = (argc > 1) ? argv[1] : "veth0";

    /* --- load BPF objects --- */
    struct bpf_object *obj_xdp = NULL, *obj_tc = NULL;
    int xsks_fd_xdp, xsks_fd_tc, prog_fd_xdp, prog_fd_tc;

    bpf_prog_load("xdp_ingress_kern.o", BPF_PROG_TYPE_XDP,
                  &obj_xdp, &prog_fd_xdp);
    xsks_fd_xdp = bpf_object__find_map_fd_by_name(obj_xdp, "xsks_map");

    bpf_prog_load("tc_egress_kern.o", BPF_PROG_TYPE_SCHED_CLS,
                  &obj_tc, &prog_fd_tc);
    xsks_fd_tc = bpf_object__find_map_fd_by_name(obj_tc, "xsks_map");

    /* attach */
    if (bpf_set_link_xdp_fd(if_nametoindex(iface),
                            prog_fd_xdp, XDP_FLAGS_SKB_MODE))
        { perror("XDP attach"); exit(1); }

    char qc[128];
    snprintf(qc, sizeof(qc), "tc qdisc add dev %s clsact 2>/dev/null", iface);
    system(qc);

    char cmd[256];
    snprintf(cmd, sizeof(cmd), "tc filter replace dev %s egress bpf da obj tc_egress_kern.o sec tc", iface);
    if (system(cmd)) { fprintf(stderr, "tc filter failed\n"); exit(1); }

    /* --- UMEM + single XSK (queue 0) --- */
    setup_umem();
    struct xsk_socket_config scfg = {
        .rx_size = NUM_FRAMES,
        .tx_size = NUM_FRAMES,
        .xdp_flags = XDP_FLAGS_SKB_MODE,
        .bind_flags = 0      /* copy-mode; kernel ignores ZC flags on veth */
    };
    if (xsk_socket__create(&xsk, iface, 0, umem, &rx, &tx, &scfg))
        { perror("xsk create"); exit(1); }

    int xsk_fd = xsk_socket__fd(xsk);

    /* insert fd into both maps (same queue id) */
    __u32 key = 0;
    bpf_map_update_elem(xsks_fd_xdp, &key, &xsk_fd, 0);
    bpf_map_update_elem(xsks_fd_tc,  &key, &xsk_fd, 0);

    printf("AF-XDP on %s queue 0 ready — polling …\n", iface);

    /* --- main loop --- */
    struct pollfd pfd = { .fd = xsk_fd, .events = POLLIN };
    uint32_t idx_rx;

    for (;;) {
        poll(&pfd, 1, /*-1 = infinite*/ -1);

        uint32_t rcvd = xsk_ring_cons__peek(&rx, 64, &idx_rx);
        for (uint32_t i = 0; i < rcvd; i++) {
            struct xdp_desc *d = (struct xdp_desc *)xsk_ring_cons__rx_desc(&rx, idx_rx + i);
            uint8_t *buf = xsk_umem__get_data(umem_base, d->addr);
            uint32_t len = d->len;

            struct meta_hdr *mh = (struct meta_hdr *)buf;
            uint8_t *payload = buf + sizeof(*mh);
            uint32_t paylen = len - sizeof(*mh);

            if (mh->dir == DIR_INGRESS)
                shape_ingress(payload, paylen);
            else
                shape_egress(payload, paylen);

            /* strip meta and forward */
            memmove(buf, payload, paylen);
            d->len = paylen;

            /* put buffer back into FQ for driver */
            uint32_t fq_idx;
            while (!xsk_ring_prod__reserve(&fq, 1, &fq_idx));
            *xsk_ring_prod__fill_addr(&fq, fq_idx) = d->addr;
            xsk_ring_prod__submit(&fq, 1);

            /* fast-path: immediately xmit unchanged pkt */
            uint32_t tx_idx;
            while (!xsk_ring_prod__reserve(&tx, 1, &tx_idx));
            struct xdp_desc *td = xsk_ring_prod__tx_desc(&tx, tx_idx);
            td->addr = d->addr;
            td->len  = paylen;
            xsk_ring_prod__submit(&tx, 1);
        }
        xsk_ring_cons__release(&rx, rcvd);
        sendto(xsk_fd, NULL, 0, 0, NULL, 0);   /* kick TX doorbell */
    }
}
