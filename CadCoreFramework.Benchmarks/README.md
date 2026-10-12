# CadCoreFramework.Benchmarks

Microbenchmarks for the hot paths in CadCoreFramework, powered by
[BenchmarkDotNet](https://benchmarkdotnet.org/).

Benchmarks target **pure-C# code only** — they do not require AutoCAD
installed, do not open a document, do not call `Autodesk.AutoCAD.*` APIs.
That keeps them runnable on CI and gets reproducible numbers.

## What's measured

| Benchmark                        | Why it matters |
| :------------------------------- | :------------- |
| `CadResultBenchmarks`            | Confirms the `CadResult<T>` pattern is cheaper than throw/catch — justifies our policy of using Result for business-logic failures. |
| `EventAggregatorBenchmarks`      | Publish cost at 1 / 10 / 100 subscribers. Guards against accidental O(n²) regressions when the dispatch loop is modified. |
| `CommandHistoryBenchmarks`       | `Record` is called from every `CadCommandBase.ExecuteCommand`. Measures steady-state allocation (should be zero after warm-up). |

## Running

```powershell
# Release build mandatory — Debug numbers are meaningless.
dotnet run --project CadCoreFramework.Benchmarks -c Release

# Pick specific benchmarks with --filter:
dotnet run --project CadCoreFramework.Benchmarks -c Release -- --filter *EventAggregator*

# Full BDN diagnostics (ETW, GC stats):
dotnet run --project CadCoreFramework.Benchmarks -c Release -- --filter * --join
```

Reports land in `BenchmarkDotNet.Artifacts/results/` (markdown + CSV).

## Adding a new benchmark

1. Create `XxxBenchmarks.cs` with `[MemoryDiagnoser]` on the class.
2. Mark methods `[Benchmark]`. Use `[Params(...)]` to sweep input sizes.
3. Keep each measurement under 1 ms if possible — BDN auto-tunes the sample
   count, but very fast methods risk timing noise under 1 ns.
4. Avoid `Console.WriteLine` inside benchmarks — the IO overhead dominates.
