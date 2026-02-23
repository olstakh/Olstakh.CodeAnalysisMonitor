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

    private static readonly string[] BaseColumnHeaders =
    [
        "Generator",
        "Invocations",
        "Avg Duration",
        "P90 Duration",
        "Total Duration",
    ];

    private const string ExceptionsColumnHeader = "Exceptions";

    /// <summary>
    /// Builds a renderable table from the given generator statistics.
    /// The "Exceptions" column is shown only when at least one generator has exceptions.
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
        var showExceptions = stats.Any(static s => s.ExceptionCount > 0);
        var columnHeaders = showExceptions
            ? [.. BaseColumnHeaders, ExceptionsColumnHeader]
            : BaseColumnHeaders;

        var sortHint = showExceptions ? "1-6" : "1-5";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold underline]Source Generator Performance[/]")
            .Caption($"[dim]Press [bold]{sortHint}[/] to sort by column • [bold]Ctrl+C[/] to exit[/]");

        AddColumns(table, columnHeaders, sortColumn, ascending);
        AddRows(table, stats, sortColumn, ascending, maxRows, showExceptions, columnHeaders.Length);

        return table;
    }

    private static void AddColumns(Table table, string[] columnHeaders, int sortColumn, bool ascending)
    {
        for (var i = 0; i < columnHeaders.Length; i++)
        {
            var header = columnHeaders[i];

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
    }

    private static void AddRows(
        Table table,
        IReadOnlyList<GeneratorStats> stats,
        int sortColumn,
        bool ascending,
        int maxRows,
        bool showExceptions,
        int columnCount)
    {
        if (stats.Count == 0)
        {
            var emptyRow = new string[columnCount];
            emptyRow[0] = "[dim]Waiting for generator events…[/]";

            for (var i = 1; i < columnCount; i++)
            {
                emptyRow[i] = string.Empty;
            }

            table.AddRow(emptyRow);
            return;
        }

        var sorted = ApplySort(stats, sortColumn, ascending);

        foreach (var stat in sorted.Take(maxRows))
        {
            var row = new List<string>
            {
                Markup.Escape(stat.Name),
                stat.InvocationCount.ToString(CultureInfo.InvariantCulture),
                FormatDuration(stat.AverageDuration),
                FormatDuration(stat.P90Duration),
                FormatDuration(stat.TotalDuration),
            };

            if (showExceptions)
            {
                row.Add(FormatExceptionCount(stat.ExceptionCount));
            }

            table.AddRow(row.ToArray());
        }
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
            6 => ascending
                ? stats.OrderBy(static s => s.ExceptionCount)
                : stats.OrderByDescending(static s => s.ExceptionCount),
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

    private static string FormatExceptionCount(int count)
    {
        var text = count.ToString(CultureInfo.InvariantCulture);
        return count > 0 ? $"[red bold]{text}[/]" : text;
    }
}
