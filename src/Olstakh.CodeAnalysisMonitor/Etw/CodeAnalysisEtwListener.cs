using System.Runtime.Versioning;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Olstakh.CodeAnalysisMonitor.Services;

namespace Olstakh.CodeAnalysisMonitor.Etw;

/// <inheritdoc />
[SupportedOSPlatform("windows")]
internal sealed class CodeAnalysisEtwListener : ICodeAnalysisEtwListener
{
    /// <summary>
    /// ETW provider name for Roslyn's CodeAnalysisEventSource.
    /// The "-General" suffix is how the EventSource manifest registers the provider.
    /// </summary>
    private const string ProviderName = "Microsoft-CodeAnalysis-General";

    /// <summary>
    /// ETW event name for the single generator run time stop event.
    /// </summary>
    private const string SingleGeneratorStopEventName = "SingleGeneratorRunTime/Stop";

    /// <summary>
    /// ETW session name. Must be unique on the system.
    /// </summary>
    private const string SessionName = "CodeAnalysisMonitor-Generators";

    /// <summary>
    /// EventSource keyword for Performance events (0b001).
    /// </summary>
    private const ulong PerformanceKeyword = 0x1;

    private readonly IGeneratorStatsAggregator _aggregator;
    private readonly TraceEventSession _session;
    private Task? _processingTask;
    private bool _disposed;

    public CodeAnalysisEtwListener(IGeneratorStatsAggregator aggregator)
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
            SingleGeneratorStopEventName,
            OnSingleGeneratorStop);

        // Process() blocks until the session is stopped, so run it on a background thread
        _processingTask = Task.Run(() => _session.Source.Process());
    }

    private void OnSingleGeneratorStop(TraceEvent data)
    {
        var generatorName = (string)data.PayloadByName("generatorName");
        var elapsedTicks = (long)data.PayloadByName("elapsedTicks");
        _aggregator.RecordInvocation(generatorName, elapsedTicks);
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
