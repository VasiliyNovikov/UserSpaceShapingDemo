using UserSpaceShapingDemo.Lib.Std;

namespace UserSpaceShapingDemo.Lib.Links;

public class LinkHardwareOffload
{
    private readonly NetNs _ns;
    private readonly string _ifName;

    public bool TXChecksum
    {
        get => Get(EthernetFeature.TXChecksumOffload);
        set => Set(EthernetFeature.TXChecksumOffload, value);
    }

    public bool RXChecksum
    {
        get => Get(EthernetFeature.RXChecksumOffload);
        set => Set(EthernetFeature.RXChecksumOffload, value);
    }

    public bool ScatterGather
    {
        get => Get(EthernetFeature.ScatterGather);
        set => Set(EthernetFeature.ScatterGather, value);
    }

    public bool TSO
    {
        get => Get(EthernetFeature.TSO);
        set => Set(EthernetFeature.TSO, value);
    }

    public bool UFO
    {
        get => Get(EthernetFeature.UFO);
        set => Set(EthernetFeature.UFO, value);
    }

    public bool GSO
    {
        get => Get(EthernetFeature.GSO);
        set => Set(EthernetFeature.GSO, value);
    }

    public bool GRO
    {
        get => Get(EthernetFeature.GRO);
        set => Set(EthernetFeature.GRO, value);
    }

    internal LinkHardwareOffload(NetNs ns, string ifName)
    {
        _ns = ns;
        _ifName = ifName;
    }

    private bool Get(EthernetFeature feature)
    {
        using (NetNs.Enter(_ns))
            return EthernetTool.Get(_ifName, feature);
    }

    private void Set(EthernetFeature feature, bool value)
    {
        using (NetNs.Enter(_ns))
            EthernetTool.Set(_ifName, feature, value);
    }
}