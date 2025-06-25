LIBS    := -lbpf -lelf -pthread
ARCH     := $(shell uname -m)
BPF_CFLAGS := -D_GNU_SOURCE -target bpf -O2 -g -Wall -I/usr/include/bpf -I/usr/include/$(ARCH)-linux-gnu
CFLAGS  := -D_GNU_SOURCE -O2 -g -Wall -I/usr/include/bpf -I/usr/include/$(ARCH)-linux-gnu

all: shaper_user

%.o: %.c
	clang $(BPF_CFLAGS) -c $< -o $@

shaper_user: shaper_user.c xdp_ingress_kern.o tc_egress_kern.o
	clang $(CFLAGS) $< -o $@ $(LIBS)

clean:
	rm -f *.o shaper_user
.PHONY: clean
