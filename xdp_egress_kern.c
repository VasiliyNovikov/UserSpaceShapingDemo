// SPDX-License-Identifier: MIT
#include <linux/bpf.h>
#include <bpf/bpf_helpers.h>
#include "meta.h"

struct { __uint(type, BPF_MAP_TYPE_XSKMAP);
    __uint(max_entries, 1);
    __type(key, __u32); __type(value, __u32); } xsks_map SEC(".maps");

static __always_inline int prepend_tag(struct xdp_md *ctx, __u8 dir)
{
    if (bpf_xdp_adjust_meta(ctx, -(int)sizeof(struct meta_hdr))) return XDP_ABORTED;
    void *data = (void *)(long)ctx->data;
    void *meta = (void *)(long)ctx->data_meta;
    if (meta + sizeof(struct meta_hdr) > data)                   return XDP_ABORTED;
    ((struct meta_hdr *)meta)->dir = dir;
    return XDP_PASS;
}

SEC("xdp")
int xdp_egress(struct xdp_md *ctx)
{
    if (prepend_tag(ctx, DIR_EGRESS) != XDP_PASS) return XDP_ABORTED;
    return bpf_redirect_map(&xsks_map, 0, 0);
}
char _license[] SEC("license") = "MIT";
