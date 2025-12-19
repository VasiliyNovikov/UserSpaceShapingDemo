namespace UserSpaceShapingDemo.Lib.Headers;

public interface IHasNextHeader
{
    public IPProtocol Protocol { get; set; }

    ref T NextHeader<T>() where T : unmanaged;
}