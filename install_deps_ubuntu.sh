#!/usr/bin/env bash
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
  echo "Please run as root (sudo $0)"; exit 1
fi

. /etc/os-release

install_required_packages() {
  apt-get install -y \
    libbpf-dev \
    libelf-dev \
    zlib1g-dev
}

install_optional_libxdp() {
  if apt-cache show libxdp-dev 2>/dev/null | grep -q '^Package: libxdp-dev$'; then
    apt-get install -y libxdp-dev
  else
    echo "libxdp-dev is not available on ${ID:-linux} ${VERSION_ID:-unknown}; relying on the legacy libbpf fallback."
  fi
}

apt-get update

install_required_packages
install_optional_libxdp

echo "Dependencies installed."
echo
echo "Tips:"
echo "  • This script installs Linux packages only; install the .NET 10 SDK/runtime separately."
echo "  • Integration tests and benchmarks still require sudo because they create namespaces, veth pairs, and AF_XDP sockets."
echo "  • libxdp is optional here: the managed interop falls back to legacy libbpf when libxdp is unavailable."
echo "  • Consider disabling GRO/LRO on your NIC for cleaner packet behavior with AF_XDP:"
echo "      ethtool -K <IFACE> gro off lro off"
echo "  • Ensure your user has the privileges needed for CAP_NET_ADMIN-style operations."
