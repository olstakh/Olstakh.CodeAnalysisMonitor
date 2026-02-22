namespace Olstakh.CodeAnalysisMonitor.Models;

/// <summary>
/// Immutable snapshot of performance statistics for a single source generator.
/// </summary>
internal sealed record GeneratorStats
{
    /// <summary>Gets the fully qualified generator type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the total number of invocations recorded.</summary>
    public required int InvocationCount { get; init; }

    /// <summary>Gets the average duration across all invocations.</summary>
    public required TimeSpan AverageDuration { get; init; }

    /// <summary>Gets the total accumulated duration across all invocations.</summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>Gets the 90th percentile duration.</summary>
    public required TimeSpan P90Duration { get; init; }
}
