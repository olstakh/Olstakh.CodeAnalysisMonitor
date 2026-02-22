using System.Globalization;
using Olstakh.CodeAnalysisMonitor.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Olstakh.CodeAnalysisMonitor.Rendering;

/// <summary>
/// Builds Spectre.Console tables for displaying generator performance statistics.
/// </summary>
internal static class GeneratorTableBuilder
{
    private const string AscendingIndicator = " ▲";
    private const string DescendingIndicator = " ▼";
    private static readonly string[] ColumnHeaders =
    [
        "Generator",
        "Invocations",
        "Avg Duration",
        "P90 Duration",
        "Total Duration",
    ];

    /// <summary>
    /// Builds a renderable table from the given generator statistics.
    /// </summary>
    /// <param name="stats">Current statistics snapshot.</param>
    /// <param name="sortColumn">1-based column index to sort by.</param>
    /// <param name="ascending">Whether the sort order is ascending.</param>
    /// <param name="maxRows">Maximum number of rows to display.</param>
    public static IRenderable Build(
        IReadOnlyList<GeneratorStats> stats,
        int sortColumn,
        bool ascending,
        int maxRows)
    {
        var sorted = ApplySort(stats, sortColumn, ascending);
        var displayed = sorted.Take(maxRows);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold underline]Source Generator Performance[/]")
            .Caption("[dim]Press [bold]1-5[/] to sort by column • [bold]Ctrl+C[/] to exit[/]");

        for (var i = 0; i < ColumnHeaders.Length; i++)
        {
            var header = ColumnHeaders[i];

            if (i + 1 == sortColumn)
            {
                header += ascending ? AscendingIndicator : DescendingIndicator;
            }

            var column = new TableColumn($"[bold]{header}[/]");

            if (i == 0)
            {
                column.NoWrap();
            }

            table.AddColumn(column);
        }

        foreach (var stat in displayed)
        {
            table.AddRow(
                Markup.Escape(stat.Name),
                stat.InvocationCount.ToString(CultureInfo.InvariantCulture),
                FormatDuration(stat.AverageDuration),
                FormatDuration(stat.P90Duration),
                FormatDuration(stat.TotalDuration));
        }

        if (stats.Count == 0)
        {
            table.AddRow(
                "[dim]Waiting for generator events…[/]",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        return table;
    }

    private static IEnumerable<GeneratorStats> ApplySort(
        IReadOnlyList<GeneratorStats> stats,
        int sortColumn,
        bool ascending)
    {
        return sortColumn switch
        {
            1 => ascending
                ? stats.OrderBy(static s => s.Name, StringComparer.OrdinalIgnoreCase)
                : stats.OrderByDescending(static s => s.Name, StringComparer.OrdinalIgnoreCase),
            2 => ascending
                ? stats.OrderBy(static s => s.InvocationCount)
                : stats.OrderByDescending(static s => s.InvocationCount),
            3 => ascending
                ? stats.OrderBy(static s => s.AverageDuration)
                : stats.OrderByDescending(static s => s.AverageDuration),
            4 => ascending
                ? stats.OrderBy(static s => s.P90Duration)
                : stats.OrderByDescending(static s => s.P90Duration),
            5 => ascending
                ? stats.OrderBy(static s => s.TotalDuration)
                : stats.OrderByDescending(static s => s.TotalDuration),
            _ => stats.OrderByDescending(static s => s.TotalDuration),
        };
    }

    private static string FormatDuration(TimeSpan ts)
    {
        return ts.TotalSeconds switch
        {
            >= 1 => $"{ts.TotalSeconds:F3}s",
            >= 0.001 => $"{ts.TotalMilliseconds:F2}ms",
            _ => $"{ts.TotalMicroseconds:F0}µs",
        };
    }
}
