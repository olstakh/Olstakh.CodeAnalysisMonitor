namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Provides an abstraction for writing live event output during a monitoring session.
/// Implementations control the destination (console, file, etc.) and whether output is enabled.
/// </summary>
#pragma warning disable CA1515 // Intentionally public for DI pattern
public interface ILiveOutputWriter
#pragma warning restore CA1515
{
    /// <summary>
    /// Gets a value indicating whether live output is enabled.
    /// Handlers can check this to skip formatting work when output is disabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Writes a timestamped event message to the output destination.
    /// </summary>
    /// <param name="timestamp">The event timestamp.</param>
    /// <param name="message">The formatted event message.</param>
    void WriteEvent(DateTime timestamp, string message);
}
