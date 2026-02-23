using Olstakh.CodeAnalysisMonitor.Export;
using Olstakh.CodeAnalysisMonitor.Models;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Export;

public sealed class CompilationCsvExporterTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void WriteSummary_WithStats_ProducesCorrectCsv()
    {
        IReadOnlyList<CompilationStats> stats =
        [
            new()
            {
                Name = "MyProject",
                CompilationCount = 5,
                AverageDuration = TimeSpan.FromSeconds(2),
                P90Duration = TimeSpan.FromSeconds(3),
                TotalDuration = TimeSpan.FromSeconds(10),
            },
        ];

        using var writer = new StringWriter();
        CompilationCsvExporter.WriteSummary(writer, stats);
        var csv = writer.ToString();

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Project,Compilations,AvgDurationMs,P90DurationMs,TotalDurationMs", lines[0]);
        Assert.Contains("MyProject", lines[1], StringComparison.Ordinal);
        Assert.Contains(",5,", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void WriteSummary_WithEmptyStats_ProducesHeaderOnly()
    {
        using var writer = new StringWriter();
        CompilationCsvExporter.WriteSummary(writer, []);
        var csv = writer.ToString();

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.StartsWith("Project,", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void WriteDetails_WithEvents_ProducesOrderedCsv()
    {
        IReadOnlyList<CompilationEvent> events =
        [
            new()
            {
                Timestamp = BaseTime.AddSeconds(10),
                ProjectName = "ProjectB",
                DurationMs = 3000.5,
            },
            new()
            {
                Timestamp = BaseTime,
                ProjectName = "ProjectA",
                DurationMs = 1500.0,
            },
        ];

        using var writer = new StringWriter();
        CompilationCsvExporter.WriteDetails(writer, events);
        var csv = writer.ToString();

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Timestamp,ProjectName,DurationMs", lines[0]);
        // ProjectA (earlier) should come first
        Assert.Contains("ProjectA", lines[1], StringComparison.Ordinal);
        Assert.Contains("1500.000", lines[1], StringComparison.Ordinal);
        // ProjectB (later) should come second
        Assert.Contains("ProjectB", lines[2], StringComparison.Ordinal);
        Assert.Contains("3000.500", lines[2], StringComparison.Ordinal);
    }
}
