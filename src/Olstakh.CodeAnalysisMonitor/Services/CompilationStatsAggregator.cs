using System.Collections.Concurrent;
using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <inheritdoc />
internal sealed class CompilationStatsAggregator : ICompilationStatsAggregator
{
    private readonly ConcurrentDictionary<string, CompilationData> _compilations = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void RecordStart(string name, DateTime timestamp)
    {
        var data = _compilations.GetOrAdd(name, static _ => new CompilationData());
        data.PendingStarts.Push(timestamp);
    }

    /// <inheritdoc />
    public void RecordStop(string name, DateTime timestamp)
    {
        var data = _compilations.GetOrAdd(name, static _ => new CompilationData());

        if (data.PendingStarts.TryPop(out var startTimestamp))
        {
            var duration = timestamp - startTimestamp;
            if (duration > TimeSpan.Zero)
            {
                data.Durations.Add(duration.TotalMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CompilationStats> GetSnapshot()
    {
        return _compilations
            .Where(static kvp => !kvp.Value.Durations.IsEmpty)
            .Select(static kvp =>
            {
                var durations = kvp.Value.Durations.ToArray();
                var count = durations.Length;

                var totalMs = 0.0;
                foreach (var d in durations)
                {
                    totalMs += d;
                }

                var avgMs = totalMs / count;

                Array.Sort(durations);
                var p90Index = Math.Max(0, (int)Math.Ceiling(count * 0.9) - 1);
                var p90Ms = durations[p90Index];

                return new CompilationStats
                {
                    Name = kvp.Key,
                    CompilationCount = count,
                    AverageDuration = TimeSpan.FromMilliseconds(avgMs),
                    P90Duration = TimeSpan.FromMilliseconds(p90Ms),
                    TotalDuration = TimeSpan.FromMilliseconds(totalMs),
                };
            })
            .ToList();
    }

    private sealed class CompilationData
    {
        /// <summary>Stack of unmatched start timestamps waiting for a stop event.</summary>
        public ConcurrentStack<DateTime> PendingStarts { get; } = new();

        /// <summary>Completed compilation durations in milliseconds.</summary>
        public ConcurrentBag<double> Durations { get; } = [];
    }
}
