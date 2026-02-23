using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <summary>
/// Aggregates Roslyn workspace operation block events and provides statistical snapshots.
/// </summary>
internal interface IWorkspaceStatsAggregator
{
    /// <summary>
    /// Registers the FunctionId → name mapping received from a SendFunctionDefinitions event.
    /// </summary>
    /// <param name="definitions">The raw definitions string from the ETW event (lines of "id name goal").</param>
    void RegisterFunctionDefinitions(string definitions);

    /// <summary>
    /// Records a completed block (BlockStop event).
    /// </summary>
    /// <param name="functionId">The FunctionId integer value identifying the operation.</param>
    /// <param name="durationMs">The block duration in milliseconds (Environment.TickCount delta).</param>
    void RecordBlockCompleted(int functionId, int durationMs);

    /// <summary>
    /// Records a canceled block (BlockCanceled event).
    /// </summary>
    /// <param name="functionId">The FunctionId integer value identifying the operation.</param>
    /// <param name="durationMs">The block duration in milliseconds (Environment.TickCount delta).</param>
    void RecordBlockCanceled(int functionId, int durationMs);

    /// <summary>
    /// Returns an immutable snapshot of current statistics for all recorded operations.
    /// </summary>
    IReadOnlyList<WorkspaceOperationStats> GetSnapshot();
}
