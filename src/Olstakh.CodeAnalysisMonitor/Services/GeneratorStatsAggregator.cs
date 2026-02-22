using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <inheritdoc />
internal sealed class GeneratorStatsAggregator : IGeneratorStatsAggregator
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, List<long>> _invocations = [];

    /// <inheritdoc />
    public void RecordInvocation(string generatorName, long elapsedTicks)
    {
        lock (_lock)
        {
            if (!_invocations.TryGetValue(generatorName, out var list))
            {
                list = [];
                _invocations[generatorName] = list;
            }

            list.Add(elapsedTicks);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<GeneratorStats> GetSnapshot()
    {
        lock (_lock)
        {
            return _invocations.Select(static kvp =>
            {
                var durations = kvp.Value;
                var count = durations.Count;

                var totalTicks = 0L;
                foreach (var d in durations)
                {
                    totalTicks += d;
                }

                var avgTicks = totalTicks / count;

                // P90: sort a copy and pick the value at the 90th percentile index
                var sorted = durations.Order().ToList();
                var p90Index = Math.Max(0, (int)Math.Ceiling(count * 0.9) - 1);
                var p90Ticks = sorted[p90Index];

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
}
