using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <summary>
/// Aggregates source generator invocation data and provides statistical snapshots.
/// </summary>
internal interface IGeneratorStatsAggregator
{
    /// <summary>
    /// Records a single generator invocation.
    /// </summary>
    /// <param name="generatorName">The fully qualified generator type name.</param>
    /// <param name="elapsedTicks">Elapsed time in <see cref="TimeSpan"/> ticks (100ns units).</param>
    void RecordInvocation(string generatorName, long elapsedTicks);

    /// <summary>
    /// Returns an immutable snapshot of current statistics for all recorded generators.
    /// </summary>
    IReadOnlyList<GeneratorStats> GetSnapshot();
}
