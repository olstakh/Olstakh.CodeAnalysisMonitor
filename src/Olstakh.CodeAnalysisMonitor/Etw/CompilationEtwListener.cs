using System.Runtime.Versioning;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Olstakh.CodeAnalysisMonitor.Services;

namespace Olstakh.CodeAnalysisMonitor.Etw;

/// <inheritdoc />
[SupportedOSPlatform("windows")]
internal sealed class CompilationEtwListener : ICompilationEtwListener
{
    /// <summary>
    /// ETW provider name for Roslyn's CodeAnalysisEventSource.
    /// Same provider as the generator listener — compilation events are in the same EventSource.
    /// </summary>
    private const string ProviderName = "Microsoft-CodeAnalysis-General";

    /// <summary>
    /// ETW event names for the server compilation start/stop pair (Task 4: Compilation).
    /// </summary>
    private const string CompilationStartEventName = "Compilation/Start";
    private const string CompilationStopEventName = "Compilation/Stop";

    /// <summary>
    /// ETW session name. Must be unique on the system.
    /// </summary>
    private const string SessionName = "CodeAnalysisMonitor-Compilations";

    /// <summary>
    /// EventSource keyword for Performance events (0b001).
    /// </summary>
    private const ulong PerformanceKeyword = 0x1;

    private readonly ICompilationStatsAggregator _aggregator;
    private readonly TraceEventSession _session;
    private Task? _processingTask;
    private bool _disposed;

    public CompilationEtwListener(ICompilationStatsAggregator aggregator)
    {
        _aggregator = aggregator;
        _session = new TraceEventSession(SessionName);
    }

    /// <inheritdoc />
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _session.EnableProvider(ProviderName, TraceEventLevel.Verbose, PerformanceKeyword);

        _session.Source.Dynamic.AddCallbackForProviderEvent(
            ProviderName,
            CompilationStartEventName,
            OnCompilationStart);

        _session.Source.Dynamic.AddCallbackForProviderEvent(
            ProviderName,
            CompilationStopEventName,
            OnCompilationStop);

        // Process() blocks until the session is stopped, so run it on a background thread
        _processingTask = Task.Run(() => _session.Source.Process());
    }

    private void OnCompilationStart(TraceEvent data)
    {
        var name = (string)data.PayloadByName("name");
        _aggregator.RecordStart(name, data.TimeStamp);
    }

    private void OnCompilationStop(TraceEvent data)
    {
        var name = (string)data.PayloadByName("name");
        _aggregator.RecordStop(name, data.TimeStamp);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.Stop();

        if (_processingTask is not null)
        {
            await _processingTask.ConfigureAwait(false);
        }

        _session.Dispose();
    }
}
