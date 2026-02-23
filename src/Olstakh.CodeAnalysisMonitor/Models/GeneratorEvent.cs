namespace Olstakh.CodeAnalysisMonitor.Models;

/// <summary>
/// A single recorded generator event with its ETW timestamp.
/// </summary>
internal sealed record GeneratorEvent
{
    /// <summary>Gets the ETW event timestamp.</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>Gets the fully qualified generator type name.</summary>
    public required string GeneratorName { get; init; }

    /// <summary>Gets the type of event.</summary>
    public required GeneratorEventType EventType { get; init; }

    /// <summary>Gets the duration in <see cref="TimeSpan"/> ticks (only set for invocation events).</summary>
    public long? DurationTicks { get; init; }
}
