/* meta.h – 4-byte header that tells userspace which direction a pkt took */
#ifndef META_H
#define META_H
struct meta_hdr {
    __u8  dir;        /* 0 = ingress, 1 = egress */
    __u8  pad[3];
};
#define DIR_INGRESS 0
#define DIR_EGRESS  1
#endif