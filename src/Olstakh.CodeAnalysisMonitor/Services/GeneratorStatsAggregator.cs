using System.Collections.Concurrent;
using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <inheritdoc />
internal sealed class GeneratorStatsAggregator : IGeneratorStatsAggregator
{
    private readonly ConcurrentDictionary<string, GeneratorData> _generators = [];
    private readonly ConcurrentBag<GeneratorEvent> _events = [];

    /// <inheritdoc />
    public void RecordInvocation(string generatorName, long elapsedTicks, DateTime timestamp)
    {
        var data = _generators.GetOrAdd(generatorName, static _ => new GeneratorData());
        data.Durations.Add(elapsedTicks);

        _events.Add(new GeneratorEvent
        {
            Timestamp = timestamp,
            GeneratorName = generatorName,
            EventType = GeneratorEventType.Invocation,
            DurationTicks = elapsedTicks,
        });
    }

    /// <inheritdoc />
    public void RecordException(string generatorName, DateTime timestamp)
    {
        var data = _generators.GetOrAdd(generatorName, static _ => new GeneratorData());
        data.ExceptionCounts.Add(1);

        _events.Add(new GeneratorEvent
        {
            Timestamp = timestamp,
            GeneratorName = generatorName,
            EventType = GeneratorEventType.Exception,
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<GeneratorStats> GetSnapshot()
    {
        return _generators.Select(static kvp =>
        {
            var data = kvp.Value;
            var durations = data.Durations.ToArray();
            var count = durations.Length;
            var exceptionCount = data.ExceptionCounts.Count;

            if (count == 0)
            {
                return new GeneratorStats
                {
                    Name = kvp.Key,
                    InvocationCount = 0,
                    AverageDuration = TimeSpan.Zero,
                    TotalDuration = TimeSpan.Zero,
                    P90Duration = TimeSpan.Zero,
                    ExceptionCount = exceptionCount,
                };
            }

            var totalTicks = 0L;
            foreach (var d in durations)
            {
                totalTicks += d;
            }

            var avgTicks = totalTicks / count;

            // P90: sort a copy and pick the value at the 90th percentile index
            Array.Sort(durations);
            var p90Index = Math.Max(0, (int)Math.Ceiling(count * 0.9) - 1);
            var p90Ticks = durations[p90Index];

            return new GeneratorStats
            {
                Name = kvp.Key,
                InvocationCount = count,
                AverageDuration = TimeSpan.FromTicks(avgTicks),
                TotalDuration = TimeSpan.FromTicks(totalTicks),
                P90Duration = TimeSpan.FromTicks(p90Ticks),
                ExceptionCount = exceptionCount,
            };
        }).ToList();
    }

    private sealed class GeneratorData
    {
        public ConcurrentBag<long> Durations { get; } = [];
        public ConcurrentBag<int> ExceptionCounts { get; } = [];
    }

    /// <inheritdoc />
    public IReadOnlyList<GeneratorEvent> GetDetailedEvents()
    {
        return [.. _events];
    }
}
