using Olstakh.CodeAnalysisMonitor.Export;
using Olstakh.CodeAnalysisMonitor.Models;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Export;

public sealed class GeneratorCsvExporterTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void WriteSummary_WithStats_ProducesCorrectCsv()
    {
        IReadOnlyList<GeneratorStats> stats =
        [
            new()
            {
                Name = "GenAlpha",
                InvocationCount = 10,
                AverageDuration = TimeSpan.FromMilliseconds(50),
                P90Duration = TimeSpan.FromMilliseconds(80),
                TotalDuration = TimeSpan.FromMilliseconds(500),
                ExceptionCount = 2,
            },
        ];

        using var writer = new StringWriter();
        GeneratorCsvExporter.WriteSummary(writer, stats);
        var csv = writer.ToString();

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Generator,Invocations,AvgDurationMs,P90DurationMs,TotalDurationMs,Exceptions", lines[0]);
        Assert.Contains("GenAlpha", lines[1], StringComparison.Ordinal);
        Assert.Contains(",10,", lines[1], StringComparison.Ordinal);
        Assert.Contains(",2", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void WriteSummary_WithEmptyStats_ProducesHeaderOnly()
    {
        using var writer = new StringWriter();
        GeneratorCsvExporter.WriteSummary(writer, []);
        var csv = writer.ToString();

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.StartsWith("Generator,", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void WriteDetails_WithEvents_ProducesOrderedCsv()
    {
        IReadOnlyList<GeneratorEvent> events =
        [
            new()
            {
                Timestamp = BaseTime.AddSeconds(2),
                GeneratorName = "Gen.B",
                EventType = GeneratorEventType.Invocation,
                DurationTicks = 5000,
            },
            new()
            {
                Timestamp = BaseTime,
                GeneratorName = "Gen.A",
                EventType = GeneratorEventType.Exception,
            },
        ];

        using var writer = new StringWriter();
        GeneratorCsvExporter.WriteDetails(writer, events);
        var csv = writer.ToString();

        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Timestamp,GeneratorName,EventType,DurationTicks", lines[0]);
        // Gen.A (earlier) should come first
        Assert.Contains("Gen.A", lines[1], StringComparison.Ordinal);
        Assert.Contains("Exception", lines[1], StringComparison.Ordinal);
        // Gen.B (later) should come second
        Assert.Contains("Gen.B", lines[2], StringComparison.Ordinal);
        Assert.Contains("5000", lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void WriteSummary_WithCommaInName_EscapesCorrectly()
    {
        IReadOnlyList<GeneratorStats> stats =
        [
            new()
            {
                Name = "Gen,WithComma",
                InvocationCount = 1,
                AverageDuration = TimeSpan.FromMilliseconds(10),
                P90Duration = TimeSpan.FromMilliseconds(10),
                TotalDuration = TimeSpan.FromMilliseconds(10),
                ExceptionCount = 0,
            },
        ];

        using var writer = new StringWriter();
        GeneratorCsvExporter.WriteSummary(writer, stats);
        var csv = writer.ToString();

        Assert.Contains("\"Gen,WithComma\"", csv, StringComparison.Ordinal);
    }
}
