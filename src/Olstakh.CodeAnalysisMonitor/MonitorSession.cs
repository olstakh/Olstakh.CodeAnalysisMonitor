using Microsoft.Diagnostics.Tracing.Session;

namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Manages an ETW trace session that listens for Roslyn Code Analysis events.
/// Supports live output, filtering, and summary aggregation.
/// </summary>
internal sealed class MonitorSession : IDisposable
{
    private const string ProviderName = "Microsoft-CodeAnalysis-General";
    private const string SessionName = "CodeAnalysisMonitor";

    private readonly TraceEventSession _session;
    private readonly EventFilter _filter;
    private readonly EventAggregator _aggregator;
    private readonly bool _live;
    private readonly IReadOnlySet<CaptureEventKind> _captureKinds;

    public MonitorSession(
        bool live,
        EventFilter filter,
        IReadOnlySet<CaptureEventKind> captureKinds)
    {
        _live = live;
        _filter = filter;
        _captureKinds = captureKinds;
        _aggregator = new EventAggregator();
        _session = new TraceEventSession(SessionName);
    }

    /// <summary>
    /// Starts the monitoring session. This method blocks until the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">Token to signal graceful shutdown (e.g., Ctrl+C).</param>
    public void Run(CancellationToken cancellationToken)
    {
        RegisterCallbacks();
        _session.EnableProvider(ProviderName);

        // Process() blocks until StopProcessing() is called, but it only checks
        // the stop flag when an event arrives. Run it on a background thread so
        // we can respond to Ctrl+C immediately by disposing the session, which
        // unblocks Process() regardless of event flow.
        var processingTask = Task.Run(() => _session.Source.Process(), CancellationToken.None);

        cancellationToken.WaitHandle.WaitOne();

        _session.Source.StopProcessing();
        _session.Stop();

        // Give Process() a moment to finish cleanly after being stopped.
        processingTask.Wait(TimeSpan.FromSeconds(3), CancellationToken.None);
    }

    /// <summary>
    /// Prints the aggregated summary of all captured events to the console.
    /// </summary>
#pragma warning disable CA1303 // CLI tool does not need localized string resources
    public void PrintSummary()
    {
        var summary = _aggregator.GetSummary();

        Console.WriteLine();
        Console.WriteLine($"=== Summary ({summary.Count} unique entries) ===");

        if (summary.Count == 0)
        {
            Console.WriteLine("  (no events were captured)");
            return;
        }

        foreach (var entry in summary)
        {
            Console.WriteLine(
                $"  {entry.Key}: {entry.TotalDuration.TotalMilliseconds:N0}ms total, {entry.Count} invocation(s)");
        }
    }
#pragma warning restore CA1303

    /// <summary>
    /// Registers ETW event callbacks for the requested capture event kinds.
    /// </summary>
    private void RegisterCallbacks()
    {
        if (_captureKinds.Contains(CaptureEventKind.SingleGeneratorRunTime))
        {
            RegisterSingleGeneratorRunTimeCallback();
        }
    }

    private void RegisterSingleGeneratorRunTimeCallback()
    {
        _session.Source.Dynamic.AddCallbackForProviderEvent(
            ProviderName,
            "SingleGeneratorRunTime/Stop",
            traceEvent =>
            {
                if (!_filter.Matches(name => traceEvent.PayloadByName(name)))
                {
                    return;
                }

                var generatorName = (string)traceEvent.PayloadByName("generatorName");
                var elapsedTicks = (long)traceEvent.PayloadByName("elapsedTicks");
                var assemblyPath = (string)traceEvent.PayloadByName("assemblyPath");

                _aggregator.Record(generatorName, elapsedTicks);

                if (_live)
                {
                    Console.WriteLine(
                        $"[{traceEvent.TimeStamp:HH:mm:ss.fff}] {generatorName}: " +
                        $"{TimeSpan.FromTicks(elapsedTicks).TotalMilliseconds:N0}ms " +
                        $"(Assembly: {assemblyPath})");
                }
            });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _session.Dispose();
    }
}
