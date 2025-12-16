using System;

namespace UserSpaceShapingDemo.Lib.Forwarding;

public interface IForwardingLogger
{
    void Log(string ifName, uint queueId, string message);
    void LogPacket(string ifName, uint queueId, string message, Span<byte> packet);
    void LogError(string ifName, uint queueId, string message, Exception error);
    void LogError(string message, Exception error);
}