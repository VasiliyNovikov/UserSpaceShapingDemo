// SPDX-License-Identifier: MIT
#include <linux/bpf.h>
#include <bpf/bpf_helpers.h>
#include "meta.h"

struct {                       /* unchanged */
    __uint(type, BPF_MAP_TYPE_XSKMAP);
    __uint(max_entries, 1);
    __type(key, __u32);
    __type(value, __u32);
} xsks_map SEC(".maps");

SEC("xdp")
int xdp_ingress(struct xdp_md *ctx)
{
    /* 1. reserve headroom */
    if (bpf_xdp_adjust_meta(ctx, -(int)sizeof(struct meta_hdr)))
        return XDP_ABORTED;

    /* 2. bounds-check */
    void *data      = (void *)(long)ctx->data;
    void *data_meta = (void *)(long)ctx->data_meta;
    if (data_meta + sizeof(struct meta_hdr) > data)
        return XDP_ABORTED;

    /* 3. write the tag */
    struct meta_hdr *mh = data_meta;
    mh->dir = DIR_INGRESS;

    /* 4. redirect to queue-0 */
    __u32 q = 0;
    return bpf_redirect_map(&xsks_map, q, 0);
}

char _license[] SEC("license") = "MIT";
