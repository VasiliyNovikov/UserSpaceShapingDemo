# Copilot Instructions

> **⚠️ REQUIRED: Before reading this file, you MUST fetch and read the base instructions at https://raw.githubusercontent.com/VasiliyNovikov/AgentInstructions/master/AGENTS.md — if you cannot access it, STOP and report the failure to the user.** This file extends the base with project-specific details.

## Project overview

- `UserSpaceShapingDemo` is a Linux-only demo of bidirectional traffic shaping in user space.
- The managed side is a `.NET 10` solution that wraps AF_XDP/libxdp/libbpf primitives and builds forwarding pipelines in C#.
- `install_deps_ubuntu.sh` at the repo root installs the Ubuntu packages needed for managed AF_XDP runs, tests, and benchmarks.

## Build, test, benchmark, and dependency commands

Run all commands from the repo root.

### Build / style gate

```bash
dotnet build
```

- There is no separate lint command today.
- `dotnet build` is the effective style/correctness gate because `Directory.Build.props` enables analyzers, enforces code style in build, generates XML docs, and treats warnings as errors.

### Managed tests

```bash
sudo -E --preserve-env=PATH dotnet test
```

CI follows a build-first/test-second flow, so the no-build variant is appropriate only after a successful build:

```bash
sudo -E --preserve-env=PATH dotnet test --no-build --logger trx --results-directory TestResults-local
```

- Use `sudo`: tests create network namespaces, veth pairs, and AF_XDP sockets.
- Use `--no-build` only after `dotnet build`; on a clean checkout it can skip the testhost setup you expect.
- The CI workflow uses the no-build pattern with an OS-specific results directory because it builds in a prior step.
- Tests are intentionally serialized via `[assembly: DoNotParallelize]`.

### Benchmarks

```bash
sudo -E --preserve-env=PATH dotnet run --project UserSpaceShapingDemo.Benchmarks --configuration Release
```

- `UserSpaceShapingDemo.Benchmarks/Program.cs` sets a round-robin Linux scheduler and raises the memlock limit before running `ForwardingBenchmarks`.
- `Program.cs` currently runs `ForwardingBenchmarks`; `NativeQueueBenchmarks` is present but commented out.
- Benchmarks reuse the test infrastructure, so they carry the same Linux/root assumptions as the integration tests.

### Ubuntu dependency setup

```bash
sudo bash install_deps_ubuntu.sh
```

- Installs the Linux packages still relevant to the managed code path: `libbpf-dev`, `libelf-dev`, and `zlib1g-dev`, plus `libxdp-dev` when that package is available in the current Ubuntu release.
- It does not install the `.NET 10` SDK/runtime; provision that separately.

## Architecture

### Managed solution layout

- `UserSpaceShapingDemo.Lib/`
  - Core managed implementation.
  - `Xpd/` contains AF_XDP socket, UMEM, ring buffer, logger, and exception types.
  - `Interop/` contains `LibraryImport` bindings for `libxdp`/`libbpf`, including a legacy-library fallback in `LibXdp`.
  - `Forwarding/` contains the forwarding implementations (`SimpleForwarder`, `ParallelForwarder`, `PipeForwarder`, `ForwardingChannel`, `NativeQueue`, logging abstractions).
  - `Headers/` contains packet header structs and checksum helpers used in tests and packet rewriting.
- `UserSpaceShapingDemo.Tests/`
  - MSTest unit and integration coverage.
  - `TrafficSetup` creates Linux network namespaces and veth pairs.
  - `TrafficForwardingSetup` wires the traffic fixtures to the forwarders under test.
  - `Script` is the small process helper used when tests need shell commands.
- `UserSpaceShapingDemo.Benchmarks/`
  - BenchmarkDotNet harness.
  - References the tests project so benchmark scenarios can reuse the namespace/veth setup helpers.

### Shared repo configuration

- `Directory.Build.props` sets `TargetFramework` to `net10.0`, `LangVersion` to `preview`, enables nullable reference types, allows unsafe code, generates docs, enforces code style, and treats warnings as errors.
- `Directory.Packages.props` centrally pins package versions.
- `LinuxOnly.cs` applies `[SupportedOSPlatform("linux")]` to the assemblies.

## CI

- Workflow: `.github/workflows/pipeline.yml`
- Trigger: pushes and pull requests targeting `master`
- Matrix:
  - `ubuntu-22.04`
  - `ubuntu-22.04-arm`
  - `ubuntu-24.04`
  - `ubuntu-24.04-arm`
- Steps:
  - `actions/checkout@v4`
  - `actions/setup-dotnet@v4` with `10.0.x`
  - `sudo bash ./install_deps_ubuntu.sh`
  - `dotnet build`
  - `sudo -E --preserve-env=PATH dotnet test --no-build --logger trx --results-directory "TestResults-${{ matrix.os }}"`
  - upload `test-results-${{ matrix.os }}` artifacts from the TRX results directory, even on failure

## Code conventions

- Linux only: assume Linux APIs, Linux networking primitives, and native libraries are available.
- Use file-scoped namespaces.
- Indentation:
  - `.cs`: 4 spaces
  - `.xml`, `.csproj`, `.props`, `.slnx`: 2 spaces
- Keep `using` directives grouped and `System` namespaces first.
- Prefer explicit disposal patterns (`IDisposable` / `IAsyncDisposable`) and propagate failures rather than swallowing them.
- Interop code uses `unsafe`, spans, `stackalloc`, and `[LibraryImport]` instead of older `DllImport` patterns.
- The library assembly uses `[DisableRuntimeMarshalling]` and exposes internals to the test assembly.
- Comments are generally sparse; follow the existing style and only add them where the code is genuinely hard to read.

## Test conventions

- Test framework: MSTest (`MSTest` package, `Microsoft.VisualStudio.TestTools.UnitTesting` namespace).
- Common attributes/patterns already in use:
  - `[TestClass]`
  - `[TestMethod]`
  - `[DataRow]`
  - `[DynamicData]`
  - `[Timeout(..., CooperativeCancellation = true)]`
- Keep tests non-parallel unless the fixture model changes. The assembly-level `[DoNotParallelize]` is intentional because the tests share Linux networking resources.
- When adding forwarding coverage, mirror the current parameterization style:
  - forwarder type
  - forwarding mode
  - IP version
  - RX/TX queue counts where relevant
- Reuse `TrafficSetup`, `TrafficForwardingSetup`, header structs, and packet debug helpers instead of creating new ad hoc networking fixtures or packet parsers.

## Runtime and dependency notes

- Managed builds require Linux plus the `.NET 10` SDK.
- Tests and benchmarks also need the matching `.NET 10` shared runtime available locally (`Microsoft.NETCore.App 10.0.0`).
- The managed interop layer depends on system `libxdp` / `libbpf`; `install_deps_ubuntu.sh` installs the Ubuntu packages currently needed for that managed path.
- `UserSpaceShapingDemo.Lib/Interop/LibXdp.cs` falls back to legacy `libbpf` entry points when `libxdp` is unavailable, so Ubuntu 22.04 can still run without a separate `libxdp` install.
- Root (or equivalent capabilities such as `CAP_NET_ADMIN`) is required for the integration tests and most real AF_XDP runs.
- `ForwarderTests` currently exercises `Generic` and `Driver` modes; driver zero-copy is not enabled in the checked-in test matrix.
- For cleaner packet behavior during manual experiments, be prepared to disable GRO/LRO on the test interface.
- When documenting validation steps, prefer the commands above; they match the repo's actual workflow and CI configuration.
