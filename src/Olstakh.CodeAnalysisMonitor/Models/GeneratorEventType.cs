namespace Olstakh.CodeAnalysisMonitor.Models;

/// <summary>
/// Types of generator events captured from ETW.
/// </summary>
internal enum GeneratorEventType
{
    /// <summary>A generator invocation completed (has duration).</summary>
    Invocation,

    /// <summary>A generator threw an exception.</summary>
    Exception,
}
