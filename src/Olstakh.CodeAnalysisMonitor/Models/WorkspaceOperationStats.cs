namespace Olstakh.CodeAnalysisMonitor.Models;

/// <summary>
/// Immutable snapshot of performance statistics for a single Roslyn workspace operation (identified by FunctionId).
/// </summary>
internal sealed record WorkspaceOperationStats
{
    /// <summary>Gets the operation name (resolved from FunctionId via SendFunctionDefinitions, or the raw integer).</summary>
    public required string OperationName { get; init; }

    /// <summary>Gets the number of blocks that completed successfully.</summary>
    public required int CompletedCount { get; init; }

    /// <summary>Gets the number of blocks that were canceled.</summary>
    public required int CanceledCount { get; init; }

    /// <summary>Gets the average duration across all completed and canceled blocks.</summary>
    public required TimeSpan AverageDuration { get; init; }

    /// <summary>Gets the 90th percentile duration.</summary>
    public required TimeSpan P90Duration { get; init; }

    /// <summary>Gets the total accumulated duration across all blocks.</summary>
    public required TimeSpan TotalDuration { get; init; }
}
