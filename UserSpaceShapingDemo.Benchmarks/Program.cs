using BenchmarkDotNet.Running;

using UserSpaceShapingDemo.Benchmarks;
using UserSpaceShapingDemo.Lib;

//LinuxScheduler.SetScheduler(LinuxScheduler.Policy.Fifo, 99);
MemoryLockLimit.SetInfinity();

BenchmarkRunner.Run<XdpForwarderBenchmarks>();