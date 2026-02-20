namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// An immutable summary entry representing aggregated metrics for a single key.
/// </summary>
/// <param name="Key">The aggregation key (e.g., generator name).</param>
/// <param name="Count">The total number of event occurrences.</param>
/// <param name="TotalTicks">The total elapsed ticks across all occurrences.</param>
internal sealed record EventSummaryEntry(string Key, int Count, long TotalTicks)
{
    /// <summary>
    /// Gets the total elapsed time as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan TotalDuration => TimeSpan.FromTicks(TotalTicks);
}
