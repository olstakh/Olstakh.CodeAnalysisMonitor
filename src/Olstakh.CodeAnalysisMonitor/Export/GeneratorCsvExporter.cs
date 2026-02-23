using System.Globalization;
using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Export;

/// <summary>
/// Writes generator statistics and detailed events to CSV format.
/// </summary>
internal static class GeneratorCsvExporter
{
    /// <summary>
    /// Writes the summary snapshot as CSV.
    /// </summary>
    public static void WriteSummary(TextWriter writer, IReadOnlyList<GeneratorStats> stats)
    {
        writer.WriteLine("Generator,Invocations,AvgDurationMs,P90DurationMs,TotalDurationMs,Exceptions");

        foreach (var s in stats)
        {
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2:F3},{3:F3},{4:F3},{5}",
                CsvEscape(s.Name),
                s.InvocationCount,
                s.AverageDuration.TotalMilliseconds,
                s.P90Duration.TotalMilliseconds,
                s.TotalDuration.TotalMilliseconds,
                s.ExceptionCount));
        }
    }

    /// <summary>
    /// Writes the detailed event log as CSV (one row per event, ordered by timestamp).
    /// </summary>
    public static void WriteDetails(TextWriter writer, IReadOnlyList<GeneratorEvent> events)
    {
        writer.WriteLine("Timestamp,GeneratorName,EventType,DurationTicks");

        foreach (var e in events.OrderBy(static e => e.Timestamp))
        {
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0:O},{1},{2},{3}",
                e.Timestamp,
                CsvEscape(e.GeneratorName),
                e.EventType,
                e.DurationTicks?.ToString(CultureInfo.InvariantCulture) ?? string.Empty));
        }
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',', StringComparison.Ordinal) ||
            value.Contains('"', StringComparison.Ordinal) ||
            value.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
