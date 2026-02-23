using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <summary>
/// Aggregates server compilation start/stop events and provides statistical snapshots.
/// </summary>
internal interface ICompilationStatsAggregator
{
    /// <summary>
    /// Records a compilation start event.
    /// </summary>
    /// <param name="name">The compilation name (project/assembly).</param>
    /// <param name="timestamp">The ETW event timestamp.</param>
    void RecordStart(string name, DateTime timestamp);

    /// <summary>
    /// Records a compilation stop event. Matches with the most recent unmatched start for the same name.
    /// </summary>
    /// <param name="name">The compilation name (project/assembly).</param>
    /// <param name="timestamp">The ETW event timestamp.</param>
    void RecordStop(string name, DateTime timestamp);

    /// <summary>
    /// Returns an immutable snapshot of current statistics for all recorded compilations.
    /// </summary>
    IReadOnlyList<CompilationStats> GetSnapshot();

    /// <summary>
    /// Returns an immutable snapshot of all individual completed compilation events.
    /// </summary>
    IReadOnlyList<CompilationEvent> GetDetailedEvents();
}
