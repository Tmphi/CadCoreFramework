using System;
using BenchmarkDotNet.Attributes;
using CadCoreFramework.Core;

namespace CadCoreFramework.Benchmarks;

/// <summary>
/// Measures CommandHistoryService.Record throughput — the service is called
/// from inside every CadCommandBase.ExecuteCommand, so per-call cost matters.
/// Confirms the bounded buffer stays allocation-free after warm-up.
/// </summary>
[MemoryDiagnoser]
public class CommandHistoryBenchmarks
{
    private CommandHistoryService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new CommandHistoryService(capacity: 50);
        // Prime the ring buffer so steady-state is measured, not amortized grow.
        for (int i = 0; i < 100; i++)
        {
            _service.Record("WARMUP", TimeSpan.FromMilliseconds(1), true);
        }
    }

    [Benchmark]
    public void Record_SingleEntry()
    {
        _service.Record("MY_COMMAND", TimeSpan.FromMilliseconds(5), true);
    }

    [Benchmark]
    public void Record_NullOrEmpty_EarlyExit()
    {
        _service.Record(string.Empty, TimeSpan.Zero, true);
    }
}
