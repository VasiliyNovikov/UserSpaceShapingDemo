using BenchmarkDotNet.Running;

using UserSpaceShapingDemo.Benchmarks;
using UserSpaceShapingDemo.Lib;

LinuxScheduler.SetScheduler(LinuxScheduler.Policy.RoundRobin, 60);
MemoryLockLimit.SetInfinity();

//BenchmarkRunner.Run<ForwardingBenchmarks>();
//BenchmarkRunner.Run<NativeEventBenchmarks>();
BenchmarkRunner.Run<NativeQueueBenchmarks>();