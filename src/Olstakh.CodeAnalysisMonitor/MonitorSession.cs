using Microsoft.Diagnostics.Tracing.Session;

namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Manages an ETW trace session that listens for Roslyn Code Analysis events.
/// Delegates event processing to registered <see cref="ICaptureHandler"/> instances.
/// </summary>
internal sealed class MonitorSession : IDisposable
{
    private const string ProviderName = "Microsoft-CodeAnalysis-General";
    private const string SessionName = "CodeAnalysisMonitor";

    private readonly TraceEventSession _session;
#pragma warning disable CA1859 // Prefer IReadOnlyList for immutability per coding guidelines
    private readonly IReadOnlyList<ICaptureHandler> _activeHandlers;
#pragma warning restore CA1859

    public MonitorSession(
        IEnumerable<ICaptureHandler> handlers,
        IReadOnlySet<CaptureEventKind> captureKinds)
    {
        _activeHandlers = handlers.Where(h => captureKinds.Contains(h.Kind)).ToList();
        _session = new TraceEventSession(SessionName);
    }

    /// <summary>
    /// Starts the monitoring session. This method blocks until the cancellation token is triggered.
    /// </summary>
    /// <param name="cancellationToken">Token to signal graceful shutdown (e.g., Ctrl+C).</param>
    public void Run(CancellationToken cancellationToken)
    {
        foreach (var handler in _activeHandlers)
        {
            handler.Register(_session.Source.Dynamic, ProviderName);
        }

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
    /// Prints the aggregated summary from all active handlers to the console.
    /// </summary>
#pragma warning disable CA1303 // CLI tool does not need localized string resources
    public void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");

        if (_activeHandlers.Count == 0)
        {
            Console.WriteLine("  (no handlers were active)");
            return;
        }

        foreach (var handler in _activeHandlers)
        {
            handler.WriteSummary(Console.Out);
            Console.WriteLine();
        }
    }
#pragma warning restore CA1303

    /// <inheritdoc />
    public void Dispose()
    {
        _session.Dispose();
    }
}
