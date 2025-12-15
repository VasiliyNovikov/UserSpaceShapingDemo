using System;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public delegate void PacketCallback(string ifName, Span<byte> data);