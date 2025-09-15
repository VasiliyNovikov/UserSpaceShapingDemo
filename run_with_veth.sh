# Create a veth pair
sudo ip link del veth0 2>/dev/null
sudo ip link add veth0 type veth peer name veth1
sudo ip link set veth0 up
sudo ip link set veth1 up

# Add IP addresses (optional, for testing)
sudo ip addr add 10.0.0.1/24 dev veth0
sudo ip addr add 10.0.0.2/24 dev veth1

# Run your shaper on veth0
sudo ./xdp_shaper -i veth0 --delay-ms 10 --drop-nth 10

# In another terminal, send traffic from veth1
# The shaper will receive packets on veth0
