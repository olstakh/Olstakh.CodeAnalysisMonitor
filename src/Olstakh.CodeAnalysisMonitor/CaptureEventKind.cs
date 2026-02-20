namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Defines the types of Roslyn Code Analysis ETW events that can be captured.
/// </summary>
internal enum CaptureEventKind
{
    /// <summary>
    /// Captures SingleGeneratorRunTime/Stop events, which report how long each
    /// individual source generator took to execute.
    /// </summary>
    SingleGeneratorRunTime,
}
