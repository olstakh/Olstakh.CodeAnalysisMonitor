using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Handles <c>SingleGeneratorRunTime/Stop</c> events (Event ID 4), tracking
/// per-generator execution times and providing an aggregated summary.
/// </summary>
internal sealed class SingleGeneratorRunTimeHandler : ICaptureHandler
{
    private readonly EventFilter _filter;
    private readonly ILiveOutputWriter _liveOutput;
    private readonly EventAggregator _aggregator = new();

    public SingleGeneratorRunTimeHandler(EventFilter filter, ILiveOutputWriter liveOutput)
    {
        _filter = filter;
        _liveOutput = liveOutput;
    }

    /// <inheritdoc />
    public CaptureEventKind Kind => CaptureEventKind.SingleGeneratorRunTime;

    /// <inheritdoc />
    public void Register(DynamicTraceEventParser parser, string providerName)
    {
        parser.AddCallbackForProviderEvent(
            providerName,
            "SingleGeneratorRunTime/Stop",
            HandleEvent);
    }

    /// <inheritdoc />
#pragma warning disable CA1303 // CLI tool does not need localized string resources
    public void WriteSummary(TextWriter writer)
    {
        var summary = _aggregator.GetSummary();

        writer.WriteLine($"--- Single Generator Run Times ({summary.Count} generator(s)) ---");

        if (summary.Count == 0)
        {
            writer.WriteLine("  (no events were captured)");
            return;
        }

        foreach (var entry in summary)
        {
            writer.WriteLine(
                $"  {entry.Key}: {entry.TotalDuration.TotalMilliseconds:N0}ms total, {entry.Count} invocation(s)");
        }
    }
#pragma warning restore CA1303

    private void HandleEvent(TraceEvent traceEvent)
    {
        if (!_filter.Matches(name => traceEvent.PayloadByName(name)))
        {
            return;
        }

        var generatorName = (string)traceEvent.PayloadByName("generatorName");
        var elapsedTicks = (long)traceEvent.PayloadByName("elapsedTicks");
        var assemblyPath = (string)traceEvent.PayloadByName("assemblyPath");

        _aggregator.Record(generatorName, elapsedTicks);

        if (_liveOutput.IsEnabled)
        {
            _liveOutput.WriteEvent(
                traceEvent.TimeStamp,
                $"{generatorName}: {TimeSpan.FromTicks(elapsedTicks).TotalMilliseconds:N0}ms " +
                $"(Assembly: {assemblyPath})");
        }
    }
}
