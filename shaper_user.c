#include <errno.h>
#include <fcntl.h>
#include <poll.h>
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <net/if.h>
#include <sys/mman.h>
#include <sys/socket.h>
#include <time.h>

#include <bpf/libbpf.h>
#include <bpf/bpf.h>
#include <linux/if_xdp.h>
#include <linux/if_link.h>
#include <bpf/xsk.h>
#include "meta.h"

#define NUM_FRAMES  4096
#define FRAME_SIZE  XSK_UMEM__DEFAULT_FRAME_SIZE
#define UMEM_SIZE   (NUM_FRAMES * FRAME_SIZE)

static struct xsk_umem *umem;
static struct xsk_ring_prod fq;
static struct xsk_ring_cons cq;
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
    uint32_t    qid   = 0;

    /* ---- load ingress object ---------------------------------------- */
    struct bpf_object *obj_in = NULL, *obj_out = NULL;
    int prog_fd_in, prog_fd_out, map_fd_in, map_fd_out;

    if (bpf_prog_load("xdp_ingress_kern.o", BPF_PROG_TYPE_XDP, &obj_in, &prog_fd_in)) {
        perror("load ingress");
        exit(1);
    }
    map_fd_in = bpf_object__find_map_fd_by_name(obj_in, "xsks_map");

    /* ---- load egress object ----------------------------------------- */
    if (bpf_prog_load("xdp_egress_kern.o", BPF_PROG_TYPE_XDP,
                      &obj_out, &prog_fd_out))
        { perror("load egress"); exit(1); }
    map_fd_out = bpf_object__find_map_fd_by_name(obj_out, "xsks_map");

    /* ---- attach: driver (in) + generic (out) ------------------------ */
    if (bpf_set_link_xdp_fd(if_nametoindex(iface), prog_fd_in,
                            XDP_FLAGS_DRV_MODE) &&
        bpf_set_link_xdp_fd(if_nametoindex(iface), prog_fd_in,
                            XDP_FLAGS_SKB_MODE))
        { perror("attach ingress"); exit(1); }

    /* detach any old generic program then attach our egress */
    bpf_set_link_xdp_fd(if_nametoindex(iface), -1, XDP_FLAGS_SKB_MODE);
    if (bpf_set_link_xdp_fd(if_nametoindex(iface), prog_fd_out,
                            XDP_FLAGS_SKB_MODE))
        { perror("attach egress"); exit(1); }

    /* ---- create UMEM + single XSK (queue-0) ------------------------- */
    setup_umem();
    struct xsk_socket_config scfg = {
        .rx_size = NUM_FRAMES, .tx_size = NUM_FRAMES,
        .xdp_flags = XDP_FLAGS_SKB_MODE, .bind_flags = 0 };
    struct xsk_socket *xsk;
    if (xsk_socket__create(&xsk, iface, qid, umem, &rx, &tx, &scfg))
        { perror("xsk_socket"); exit(1); }
    int xsk_fd = xsk_socket__fd(xsk);

    /* ---- insert same fd into BOTH maps ------------------------------ */
    bpf_map_update_elem(map_fd_in,  &qid, &xsk_fd, 0);
    bpf_map_update_elem(map_fd_out, &qid, &xsk_fd, 0);

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
