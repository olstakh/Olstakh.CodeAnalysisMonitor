using System.Security.Principal;
using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Rendering;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;

namespace Olstakh.CodeAnalysisMonitor.Commands;

/// <summary>
/// Handles the "generator" command: captures ETW events and renders a live performance table.
/// </summary>
internal static class GeneratorCommandHandler
{
    private const int RefreshIntervalMs = 100;
    private const int DefaultSortColumn = 5; // Total Duration

    /// <summary>
    /// Runs the generator monitoring loop.
    /// </summary>
    /// <returns>Exit code: 0 on success, 1 on error.</returns>
    public static async Task<int> ExecuteAsync(int top, CancellationToken cancellationToken)
    {
        if (!IsRunningAsAdministrator())
        {
            AnsiConsole.MarkupLine(
                "[red bold]Error:[/] This tool requires [bold]administrator privileges[/] to capture ETW events.");
            AnsiConsole.MarkupLine("[dim]Please re-run from an elevated terminal.[/]");
            return 1;
        }

        var aggregator = new GeneratorStatsAggregator();
        await using var listener = new CodeAnalysisEtwListener(aggregator);
        listener.Start();

        var sortColumn = DefaultSortColumn;
        var ascending = false;

        AnsiConsole.Clear();

        var initialTable = GeneratorTableBuilder.Build([], sortColumn, ascending, top);

        await AnsiConsole.Live(initialTable)
            .AutoClear(true)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ProcessKeyboardInput(ref sortColumn, ref ascending);

                    var snapshot = aggregator.GetSnapshot();
                    var table = GeneratorTableBuilder.Build(snapshot, sortColumn, ascending, top);
                    ctx.UpdateTarget(table);

                    try
                    {
                        await Task.Delay(RefreshIntervalMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });

        return 0;
    }

    private static void ProcessKeyboardInput(ref int sortColumn, ref bool ascending)
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.KeyChar is < '1' or > '5')
            {
                continue;
            }

            var newColumn = key.KeyChar - '0';

            if (newColumn == sortColumn)
            {
                ascending = !ascending;
            }
            else
            {
                sortColumn = newColumn;
                ascending = false;
            }
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
