using BenchmarkDotNet.Running;

using LinuxCore;

using UserSpaceShapingDemo.Benchmarks;

LinuxScheduler.Set(LinuxScheduler.Policy.RoundRobin, 60);
LinuxResourceLimit.Set(LinuxResourceLimit.Resource.MemoryLock, LinuxResourceLimit.Infinity, LinuxResourceLimit.Infinity);

BenchmarkRunner.Run<ForwardingBenchmarks>();
//BenchmarkRunner.Run<NativeQueueBenchmarks>();