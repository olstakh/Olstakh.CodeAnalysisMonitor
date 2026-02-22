namespace Olstakh.CodeAnalysisMonitor.Etw;

/// <summary>
/// Listens for Roslyn CodeAnalysis ETW events and feeds data to an aggregator.
/// </summary>
internal interface ICodeAnalysisEtwListener : IDisposable
{
    /// <summary>
    /// Starts the ETW session and begins capturing events.
    /// </summary>
    void Start();
}
