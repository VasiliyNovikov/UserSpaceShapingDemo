using System;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public delegate void PacketCallback(string ifName, uint queueId, Span<byte> data);