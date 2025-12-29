using BenchmarkDotNet.Running;

using LinuxCore;

using UserSpaceShapingDemo.Benchmarks;
using UserSpaceShapingDemo.Lib;

LinuxScheduler.Set(LinuxScheduler.Policy.RoundRobin, 60);
MemoryLockLimit.SetInfinity();

BenchmarkRunner.Run<ForwardingBenchmarks>();
//BenchmarkRunner.Run<NativeEventBenchmarks>();
//BenchmarkRunner.Run<NativeQueueBenchmarks>();