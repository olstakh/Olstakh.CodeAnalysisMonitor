using System.Globalization;
using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Export;

/// <summary>
/// Writes compilation statistics and detailed events to CSV format.
/// </summary>
internal static class CompilationCsvExporter
{
    /// <summary>
    /// Writes the summary snapshot as CSV.
    /// </summary>
    public static void WriteSummary(TextWriter writer, IReadOnlyList<CompilationStats> stats)
    {
        writer.WriteLine("Project,Compilations,AvgDurationMs,P90DurationMs,TotalDurationMs");

        foreach (var s in stats)
        {
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2:F3},{3:F3},{4:F3}",
                CsvEscape(s.Name),
                s.CompilationCount,
                s.AverageDuration.TotalMilliseconds,
                s.P90Duration.TotalMilliseconds,
                s.TotalDuration.TotalMilliseconds));
        }
    }

    /// <summary>
    /// Writes the detailed event log as CSV (one row per completed compilation, ordered by timestamp).
    /// </summary>
    public static void WriteDetails(TextWriter writer, IReadOnlyList<CompilationEvent> events)
    {
        writer.WriteLine("Timestamp,ProjectName,DurationMs");

        foreach (var e in events.OrderBy(static e => e.Timestamp))
        {
            writer.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0:O},{1},{2:F3}",
                e.Timestamp,
                CsvEscape(e.ProjectName),
                e.DurationMs));
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
