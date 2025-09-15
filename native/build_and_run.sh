# 1) Install dependencies (Ubuntu)
sudo bash install_deps_ubuntu.sh

# 2) Build
make

# 3) Run (requires root)
sudo ./xdp_shaper -i eth0 -q 0 --delay-ms 10 --drop-nth 10

# Stop with Ctrl+C