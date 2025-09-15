# Build with: make
# Clean with: make clean

USER_CC ?= gcc

CFLAGS  ?= -O2 -g -Wall -Wextra
LDFLAGS ?= -lbpf -lelf -lz

all: xdp_shaper

xdp_shaper: xdp_shaper_user.c
	$(USER_CC) $(CFLAGS) -o $@ xdp_shaper_user.c $(LDFLAGS)

clean:
	rm -f xdp_shaper
