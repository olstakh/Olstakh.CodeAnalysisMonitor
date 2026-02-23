namespace Olstakh.CodeAnalysisMonitor.Models;

/// <summary>
/// Immutable snapshot of performance statistics for a single named server compilation.
/// </summary>
internal sealed record CompilationStats
{
    /// <summary>Gets the compilation name (typically the project/assembly being compiled).</summary>
    public required string Name { get; init; }

    /// <summary>Gets the total number of completed compilations.</summary>
    public required int CompilationCount { get; init; }

    /// <summary>Gets the average duration across all completed compilations.</summary>
    public required TimeSpan AverageDuration { get; init; }

    /// <summary>Gets the 90th percentile duration.</summary>
    public required TimeSpan P90Duration { get; init; }

    /// <summary>Gets the total accumulated duration across all completed compilations.</summary>
    public required TimeSpan TotalDuration { get; init; }
}
