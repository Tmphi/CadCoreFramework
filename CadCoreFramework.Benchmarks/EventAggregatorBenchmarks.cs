using BenchmarkDotNet.Attributes;
using CadCoreFramework.Core;

namespace CadCoreFramework.Benchmarks;

/// <summary>
/// Measures Publish cost as subscriber count grows. Confirms the aggregator
/// has O(n) publish behavior (not O(n²)) and bounds the per-publish overhead.
/// </summary>
[MemoryDiagnoser]
public class EventAggregatorBenchmarks
{
    private sealed record OrderCreated(int Id);

    private EventAggregator _aggregator = null!;
    private int _counter;

    [Params(1, 10, 100)]
    public int SubscriberCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _aggregator = new EventAggregator();
        for (int i = 0; i < SubscriberCount; i++)
        {
            _aggregator.Subscribe<OrderCreated>(_ => _counter++);
        }
    }

    [Benchmark]
    public void Publish_OrderCreated()
    {
        _aggregator.Publish(new OrderCreated(1));
    }
}
