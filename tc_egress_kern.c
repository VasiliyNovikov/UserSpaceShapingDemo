// SPDX-License-Identifier: MIT
#include <linux/bpf.h>
#include <linux/pkt_cls.h>
#include <bpf/bpf_helpers.h>
#include "meta.h"

struct {
    __uint(type, BPF_MAP_TYPE_XSKMAP);
    __uint(max_entries, 1);
    __type(key, __u32);
    __type(value, __u32);
} xsks_map SEC(".maps");

SEC("tc")
int tc_egress(struct __sk_buff *skb)
{
    /* 1. reserve headroom */
    if (bpf_skb_change_head(skb, -(int)sizeof(struct meta_hdr), 0))
        return TC_ACT_SHOT;

    /* 2. bounds-check                                    */
    void *data      = (void *)(long)skb->data;
    void *data_end  = (void *)(long)skb->data_end;
    if (data + sizeof(struct meta_hdr) > data_end)
        return TC_ACT_SHOT;

    /* 3. write the tag                                   */
    struct meta_hdr *mh = data;
    mh->dir = DIR_EGRESS;

    /* 4. redirect to queue-0 */
    __u32 q = 0;
    if (bpf_map_lookup_elem(&xsks_map, &q))
        return bpf_redirect_map(&xsks_map, q, 0);

    return TC_ACT_OK;
}

char _license[] SEC("license") = "MIT";
