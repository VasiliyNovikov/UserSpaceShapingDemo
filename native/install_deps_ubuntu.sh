#!/usr/bin/env bash
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
  echo "Please run as root (sudo $0)"; exit 1
fi

apt-get update

# Toolchain + headers + libbpf and friends
apt-get install -y \
  build-essential \
  libbpf-dev \
  libelf-dev \
  zlib1g-dev \
  libnl-3-dev \
  libnl-route-3-dev \
  pkg-config \
  linux-headers-$(uname -r) \
  linux-tools-$(uname -r) \
  linux-libc-dev

# Create asm symlink if it doesn't exist
if [ ! -e /usr/include/asm ]; then
  ln -s /usr/include/$(uname -m)-linux-gnu/asm /usr/include/asm
fi

echo "Dependencies installed."
echo
echo "Tips:"
echo "  • Consider disabling GRO/LRO on your NIC for cleaner packet behavior with AF_XDP:"
echo "      ethtool -K <IFACE> gro off lro off"
echo "  • Ensure you run the shaper as root and your user has CAP_NET_ADMIN if needed."
