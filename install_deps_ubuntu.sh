#!/usr/bin/env bash
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
  echo "Please run as root (sudo $0)"; exit 1
fi

apt-get update

# Managed AF_XDP runtime dependencies used by the .NET library/tests/benchmarks.
apt-get install -y \
  libxdp-dev \
  libbpf-dev \
  libelf-dev \
  zlib1g-dev

echo "Dependencies installed."
echo
echo "Tips:"
echo "  • This script installs Linux packages only; install the .NET 10 SDK/runtime separately."
echo "  • Integration tests and benchmarks still require sudo because they create namespaces, veth pairs, and AF_XDP sockets."
echo "  • Consider disabling GRO/LRO on your NIC for cleaner packet behavior with AF_XDP:"
echo "      ethtool -K <IFACE> gro off lro off"
echo "  • Ensure your user has the privileges needed for CAP_NET_ADMIN-style operations."
