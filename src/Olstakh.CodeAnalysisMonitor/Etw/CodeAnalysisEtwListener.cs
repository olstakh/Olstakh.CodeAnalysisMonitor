using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Olstakh.CodeAnalysisMonitor.Services;

namespace Olstakh.CodeAnalysisMonitor.Etw;

/// <inheritdoc />
internal sealed class CodeAnalysisEtwListener : ICodeAnalysisEtwListener
{
    /// <summary>
    /// ETW provider name for Roslyn's CodeAnalysisEventSource.
    /// </summary>
    private const string ProviderName = "Microsoft-CodeAnalysis";

    /// <summary>
    /// ETW session name. Must be unique on the system.
    /// </summary>
    private const string SessionName = "CodeAnalysisMonitor-Generators";

    /// <summary>
    /// EventSource keyword for Performance events (0b001).
    /// </summary>
    private const ulong PerformanceKeyword = 0x1;

    /// <summary>
    /// Event ID for StopSingleGeneratorRunTime (event 4).
    /// </summary>
    private const int StopSingleGeneratorRunTimeEventId = 4;

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
        _session.Source.Dynamic.All += OnEvent;

        // Process() blocks until the session is stopped, so run it on a background thread
        _processingTask = Task.Run(() => _session.Source.Process());
    }

    private void OnEvent(TraceEvent data)
    {
        if ((int)data.ID != StopSingleGeneratorRunTimeEventId)
        {
            return;
        }

        var generatorName = (string)data.PayloadByName("generatorName");
        var elapsedTicks = (long)data.PayloadByName("elapsedTicks");
        _aggregator.RecordInvocation(generatorName, elapsedTicks);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session.Stop();
        _session.Dispose();
    }
}
