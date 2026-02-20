using Microsoft.Diagnostics.Tracing.Parsers;

namespace Olstakh.CodeAnalysisMonitor;

/// <summary>
/// Defines the contract for handling a specific type of Code Analysis ETW event.
/// Each implementation registers for one or more ETW events, processes incoming
/// data, and produces a summary when the monitoring session ends.
/// </summary>
/// <remarks>
/// To add support for a new event type:
/// <list type="number">
/// <item>Add a new <see cref="CaptureEventKind"/> enum value.</item>
/// <item>Create a new <see cref="ICaptureHandler"/> implementation (internal sealed).</item>
/// <item>Register it in <see cref="ServiceCollectionExtensions.AddCodeAnalysisMonitor"/>.</item>
/// </list>
/// </remarks>
#pragma warning disable CA1515 // Intentionally public for DI pattern
public interface ICaptureHandler
#pragma warning restore CA1515
{
    /// <summary>
    /// Gets the <see cref="CaptureEventKind"/> this handler services.
    /// </summary>
    CaptureEventKind Kind { get; }

    /// <summary>
    /// Registers ETW event callbacks on the given parser for the specified provider.
    /// Called once when the monitoring session starts.
    /// </summary>
    /// <param name="parser">The dynamic trace event parser to register callbacks on.</param>
    /// <param name="providerName">The ETW provider name (e.g., "Microsoft-CodeAnalysis-General").</param>
    void Register(DynamicTraceEventParser parser, string providerName);

    /// <summary>
    /// Writes this handler's summary section to the given writer.
    /// Called once when the monitoring session ends.
    /// </summary>
    /// <param name="writer">The text writer to write summary output to.</param>
    void WriteSummary(TextWriter writer);
}
