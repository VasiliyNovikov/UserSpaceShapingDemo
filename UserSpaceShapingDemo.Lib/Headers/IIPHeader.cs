using NetworkingPrimitivesCore;

namespace UserSpaceShapingDemo.Lib.Headers;

public interface IIPHeader<TAddress> where TAddress : unmanaged, IIPAddress<TAddress>
{
    public byte Version { get; set; }
    public byte TrafficClass { get; set; }
    public byte HeaderLength { get; set; }
    public ushort PayloadLength { get; set; }
    public ushort TotalLength { get; set; }
    public IPProtocol Protocol { get; set; }
    public byte Ttl { get; set; }
    public TAddress SourceAddress { get; set; }
    public TAddress DestinationAddress { get; set; }

    ref T Layer3Header<T>() where T : unmanaged;
    void UpdateChecksum();
}