namespace UserSpaceShapingDemo.Lib.Std;

public readonly struct EthernetChannels(uint tx, uint rx, uint other, uint combined)
{
    public uint TX => tx;
    public uint RX => rx;
    public uint Other => other;
    public uint Combined => combined;
}