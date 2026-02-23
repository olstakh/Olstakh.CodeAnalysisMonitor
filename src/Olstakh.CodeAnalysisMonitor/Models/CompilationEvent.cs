namespace Olstakh.CodeAnalysisMonitor.Models;

/// <summary>
/// A single recorded completed compilation event with timing information.
/// </summary>
internal sealed record CompilationEvent
{
    /// <summary>Gets the timestamp when the compilation completed (stop event).</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>Gets the compilation name (project/assembly).</summary>
    public required string ProjectName { get; init; }

    /// <summary>Gets the compilation duration in milliseconds.</summary>
    public required double DurationMs { get; init; }
}
