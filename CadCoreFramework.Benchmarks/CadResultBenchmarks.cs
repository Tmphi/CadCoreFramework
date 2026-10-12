using System;
using BenchmarkDotNet.Attributes;
using CadCoreFramework.Core;

namespace CadCoreFramework.Benchmarks;

/// <summary>
/// Measures the allocation + call-site cost of CadResult&lt;T&gt; vs returning a
/// raw value and vs throwing an exception. Confirms that the Result pattern
/// is a cheap substitute for exception-based control flow.
/// </summary>
[MemoryDiagnoser]
public class CadResultBenchmarks
{
    [Benchmark(Baseline = true)]
    public int ReturnRawInt() => 42;

    [Benchmark]
    public CadResult<int> ReturnCadResultSuccess() => CadResult<int>.Success(42);

    [Benchmark]
    public CadResult<int> ReturnCadResultFail() =>
        CadResult<int>.Fail("something went wrong");

    [Benchmark]
    public int ThrowCatch()
    {
        try { throw new InvalidOperationException("boom"); }
        catch { return 0; }
    }
}
