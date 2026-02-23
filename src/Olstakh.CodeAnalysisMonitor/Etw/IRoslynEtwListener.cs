namespace Olstakh.CodeAnalysisMonitor.Etw;

/// <summary>
/// Listens for Roslyn RoslynEventSource ETW events (block start/stop/canceled)
/// and feeds data to a workspace stats aggregator.
/// </summary>
internal interface IRoslynEtwListener : IAsyncDisposable
{
    /// <summary>
    /// Starts the ETW session and begins capturing events.
    /// </summary>
    void Start();
}
