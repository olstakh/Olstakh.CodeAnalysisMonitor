namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Writes live event output to the console with timestamps.
/// </summary>
internal sealed class ConsoleLiveOutputWriter : ILiveOutputWriter
{
    private readonly bool _enabled;

    public ConsoleLiveOutputWriter(bool enabled)
    {
        _enabled = enabled;
    }

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <inheritdoc />
#pragma warning disable CA1303 // CLI tool does not need localized string resources
    public void WriteEvent(DateTime timestamp, string message)
    {
        if (_enabled)
        {
            Console.WriteLine($"[{timestamp:HH:mm:ss.fff}] {message}");
        }
    }
#pragma warning restore CA1303
}
