using System.Globalization;
using Olstakh.CodeAnalysisMonitor.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Olstakh.CodeAnalysisMonitor.Rendering;

/// <summary>
/// Builds Spectre.Console tables for displaying server compilation statistics.
/// </summary>
internal static class CompilationTableBuilder
{
    private const string AscendingIndicator = " ▲";
    private const string DescendingIndicator = " ▼";
    private static readonly string[] ColumnHeaders =
    [
        "Compilation",
        "Count",
        "Avg Duration",
        "P90 Duration",
        "Total Duration",
    ];

    /// <summary>
    /// Builds a renderable table from the given compilation statistics.
    /// </summary>
    /// <param name="stats">Current statistics snapshot.</param>
    /// <param name="sortColumn">1-based column index to sort by.</param>
    /// <param name="ascending">Whether the sort order is ascending.</param>
    /// <param name="maxRows">Maximum number of rows to display.</param>
    public static IRenderable Build(
        IReadOnlyList<CompilationStats> stats,
        int sortColumn,
        bool ascending,
        int maxRows)
    {
        var sorted = ApplySort(stats, sortColumn, ascending);
        var displayed = sorted.Take(maxRows);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold underline]Server Compilation Performance[/]")
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
                stat.CompilationCount.ToString(CultureInfo.InvariantCulture),
                FormatDuration(stat.AverageDuration),
                FormatDuration(stat.P90Duration),
                FormatDuration(stat.TotalDuration));
        }

        if (stats.Count == 0)
        {
            table.AddRow(
                "[dim]Waiting for compilation events…[/]",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        return table;
    }

    private static IEnumerable<CompilationStats> ApplySort(
        IReadOnlyList<CompilationStats> stats,
        int sortColumn,
        bool ascending)
    {
        return sortColumn switch
        {
            1 => ascending
                ? stats.OrderBy(static s => s.Name, StringComparer.OrdinalIgnoreCase)
                : stats.OrderByDescending(static s => s.Name, StringComparer.OrdinalIgnoreCase),
            2 => ascending
                ? stats.OrderBy(static s => s.CompilationCount)
                : stats.OrderByDescending(static s => s.CompilationCount),
            3 => ascending
                ? stats.OrderBy(static s => s.AverageDuration)
                : stats.OrderByDescending(static s => s.AverageDuration),
            4 => ascending
                ? stats.OrderBy(static s => s.P90Duration)
                : stats.OrderByDescending(static s => s.P90Duration),
            _ => ascending
                ? stats.OrderBy(static s => s.TotalDuration)
                : stats.OrderByDescending(static s => s.TotalDuration),
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1)
        {
            return $"{duration.TotalMicroseconds:F0} μs";
        }

        if (duration.TotalSeconds < 1)
        {
            return $"{duration.TotalMilliseconds:F1} ms";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:F2} s";
        }

        return duration.ToString(@"m\:ss\.fff", CultureInfo.InvariantCulture);
    }
}
