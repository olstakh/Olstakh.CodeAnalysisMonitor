using System.Collections.Concurrent;
using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <inheritdoc />
internal sealed class GeneratorStatsAggregator : IGeneratorStatsAggregator
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _invocations = [];

    /// <inheritdoc />
    public void RecordInvocation(string generatorName, long elapsedTicks)
    {
        var bag = _invocations.GetOrAdd(generatorName, static _ => []);
        bag.Add(elapsedTicks);
    }

    /// <inheritdoc />
    public IReadOnlyList<GeneratorStats> GetSnapshot()
    {
        return _invocations.Select(static kvp =>
        {
            var durations = kvp.Value.ToArray();
            var count = durations.Length;

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
            };
        }).ToList();
    }
}
