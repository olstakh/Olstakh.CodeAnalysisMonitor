using System.Globalization;
using Olstakh.CodeAnalysisMonitor.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Olstakh.CodeAnalysisMonitor.Rendering;

/// <summary>
/// Builds Spectre.Console tables for displaying Roslyn workspace operation statistics.
/// </summary>
internal static class WorkspaceTableBuilder
{
    private const string AscendingIndicator = " ▲";
    private const string DescendingIndicator = " ▼";
    private static readonly string[] ColumnHeaders =
    [
        "Operation",
        "Completed",
        "Canceled",
        "Avg Duration",
        "P90 Duration",
        "Total Duration",
    ];

    /// <summary>
    /// Builds a renderable table from the given workspace operation statistics.
    /// </summary>
    /// <param name="stats">Current statistics snapshot.</param>
    /// <param name="sortColumn">1-based column index to sort by.</param>
    /// <param name="ascending">Whether the sort order is ascending.</param>
    /// <param name="maxRows">Maximum number of rows to display.</param>
    public static IRenderable Build(
        IReadOnlyList<WorkspaceOperationStats> stats,
        int sortColumn,
        bool ascending,
        int maxRows)
    {
        var sorted = ApplySort(stats, sortColumn, ascending);
        var displayed = sorted.Take(maxRows);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold underline]Roslyn Workspace Operations[/]")
            .Caption("[dim]Press [bold]1-6[/] to sort by column • [bold]Ctrl+C[/] to exit[/]");

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
                Markup.Escape(stat.OperationName),
                stat.CompletedCount.ToString(CultureInfo.InvariantCulture),
                stat.CanceledCount.ToString(CultureInfo.InvariantCulture),
                FormatDuration(stat.AverageDuration),
                FormatDuration(stat.P90Duration),
                FormatDuration(stat.TotalDuration));
        }

        if (stats.Count == 0)
        {
            table.AddRow(
                "[dim]Waiting for workspace events…[/]",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        return table;
    }

    private static IEnumerable<WorkspaceOperationStats> ApplySort(
        IReadOnlyList<WorkspaceOperationStats> stats,
        int sortColumn,
        bool ascending)
    {
        return sortColumn switch
        {
            1 => ascending
                ? stats.OrderBy(static s => s.OperationName, StringComparer.OrdinalIgnoreCase)
                : stats.OrderByDescending(static s => s.OperationName, StringComparer.OrdinalIgnoreCase),
            2 => ascending
                ? stats.OrderBy(static s => s.CompletedCount)
                : stats.OrderByDescending(static s => s.CompletedCount),
            3 => ascending
                ? stats.OrderBy(static s => s.CanceledCount)
                : stats.OrderByDescending(static s => s.CanceledCount),
            4 => ascending
                ? stats.OrderBy(static s => s.AverageDuration)
                : stats.OrderByDescending(static s => s.AverageDuration),
            5 => ascending
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
