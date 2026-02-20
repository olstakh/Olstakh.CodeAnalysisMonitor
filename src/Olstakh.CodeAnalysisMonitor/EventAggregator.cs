using System.Collections.Concurrent;

namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Thread-safe aggregator that accumulates event counts and elapsed ticks per key.
/// Used to produce a summary report when the monitoring session ends.
/// </summary>
internal sealed class EventAggregator
{
    private readonly ConcurrentDictionary<string, (int Count, long Ticks)> _data = new(StringComparer.Ordinal);

    /// <summary>
    /// Records a single event occurrence for the given key.
    /// </summary>
    /// <param name="key">The aggregation key (e.g., generator name).</param>
    /// <param name="elapsedTicks">The elapsed ticks for this event occurrence.</param>
    public void Record(string key, long elapsedTicks)
    {
        _data.AddOrUpdate(
            key,
            static (_, arg) => (1, arg),
            static (_, existing, arg) => (existing.Count + 1, existing.Ticks + arg),
            elapsedTicks);
    }

    /// <summary>
    /// Returns the aggregated summary, ordered by total elapsed time descending.
    /// </summary>
    public IReadOnlyList<EventSummaryEntry> GetSummary()
    {
        return _data
            .Select(static x => new EventSummaryEntry(x.Key, x.Value.Count, x.Value.Ticks))
            .OrderByDescending(static x => x.TotalTicks)
            .ToList();
    }
}
