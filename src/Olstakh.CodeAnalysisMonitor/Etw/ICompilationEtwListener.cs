namespace Olstakh.CodeAnalysisMonitor.Etw;

/// <summary>
/// Listens for VBCSCompiler server compilation ETW events (start/stop)
/// and feeds data to a compilation stats aggregator.
/// </summary>
internal interface ICompilationEtwListener : IAsyncDisposable
{
    /// <summary>
    /// Starts the ETW session and begins capturing events.
    /// </summary>
    void Start();
}
