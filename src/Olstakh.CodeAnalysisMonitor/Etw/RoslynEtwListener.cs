using System.Runtime.Versioning;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Olstakh.CodeAnalysisMonitor.Services;

namespace Olstakh.CodeAnalysisMonitor.Etw;

/// <inheritdoc />
[SupportedOSPlatform("windows")]
internal sealed class RoslynEtwListener : IRoslynEtwListener
{
    /// <summary>
    /// ETW provider GUID for Roslyn's RoslynEventSource.
    /// Documented in the Roslyn source: [EventSource(Name = "RoslynEventSource")]
    /// with GUID {bf965e67-c7fb-5c5b-d98f-cdf68f8154c2}.
    /// We use the GUID directly because this EventSource-based provider is not
    /// registered on the system (unlike manifest-based providers such as Microsoft-CodeAnalysis-General).
    /// </summary>
    private static readonly Guid ProviderGuid = new("bf965e67-c7fb-5c5b-d98f-cdf68f8154c2");

    /// <summary>
    /// The EventSource name, used for matching events in TraceEvent callbacks.
    /// </summary>
    private const string ProviderName = "RoslynEventSource";

    /// <summary>
    /// ETW event names matching the RoslynEventSource method names.
    /// </summary>
    private const string BlockStopEventName = "BlockStop";
    private const string BlockCanceledEventName = "BlockCanceled";
    private const string SendFunctionDefinitionsEventName = "SendFunctionDefinitions";

    /// <summary>
    /// ETW session name. Must be unique on the system.
    /// </summary>
    private const string SessionName = "CodeAnalysisMonitor-Workspace";

    private readonly IWorkspaceStatsAggregator _aggregator;
    private readonly TraceEventSession _session;
    private Task? _processingTask;
    private bool _disposed;

    public RoslynEtwListener(IWorkspaceStatsAggregator aggregator)
    {
        _aggregator = aggregator;
        _session = new TraceEventSession(SessionName);
    }

    /// <inheritdoc />
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Enable at Verbose level with all keywords to capture all RoslynEventSource events.
        // Must use the GUID (not the string name) because this is an EventSource-based provider
        // that is not registered in the system ETW provider catalog.
        _session.EnableProvider(ProviderGuid, TraceEventLevel.Verbose, ulong.MaxValue);

        _session.Source.Dynamic.AddCallbackForProviderEvent(
            ProviderName,
            BlockStopEventName,
            OnBlockStop);

        _session.Source.Dynamic.AddCallbackForProviderEvent(
            ProviderName,
            BlockCanceledEventName,
            OnBlockCanceled);

        _session.Source.Dynamic.AddCallbackForProviderEvent(
            ProviderName,
            SendFunctionDefinitionsEventName,
            OnSendFunctionDefinitions);

        // Process() blocks until the session is stopped, so run it on a background thread
        _processingTask = Task.Run(() => _session.Source.Process());
    }

    private void OnBlockStop(TraceEvent data)
    {
        // BlockStop(FunctionId functionId, int tick, int blockId)
        // Payload order matches parameter order: functionId (int), tick (int), blockId (int)
        var functionId = (int)data.PayloadByName("functionId");
        var tick = (int)data.PayloadByName("tick");
        _aggregator.RecordBlockCompleted(functionId, tick);
    }

    private void OnBlockCanceled(TraceEvent data)
    {
        // BlockCanceled(FunctionId functionId, int tick, int blockId)
        var functionId = (int)data.PayloadByName("functionId");
        var tick = (int)data.PayloadByName("tick");
        _aggregator.RecordBlockCanceled(functionId, tick);
    }

    private void OnSendFunctionDefinitions(TraceEvent data)
    {
        // SendFunctionDefinitions(string definitions)
        var definitions = (string)data.PayloadByName("definitions");
        if (!string.IsNullOrEmpty(definitions))
        {
            _aggregator.RegisterFunctionDefinitions(definitions);
        }
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
