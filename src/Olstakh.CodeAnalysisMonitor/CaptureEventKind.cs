namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Defines the types of Roslyn Code Analysis ETW events that can be captured.
/// </summary>
#pragma warning disable CA1515 // Intentionally public — exposed via ICaptureHandler.Kind
public enum CaptureEventKind
#pragma warning restore CA1515
{
    /// <summary>
    /// Captures SingleGeneratorRunTime/Stop events, which report how long each
    /// individual source generator took to execute.
    /// </summary>
    SingleGeneratorRunTime,
}
