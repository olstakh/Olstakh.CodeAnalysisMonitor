using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Rendering;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;

namespace Olstakh.CodeAnalysisMonitor.Commands;

/// <summary>
/// Handles the "workspace" command: captures RoslynEventSource ETW events and renders a live operations table.
/// </summary>
internal sealed class WorkspaceCommandHandler
{
    private const int RefreshIntervalMs = 100;
    private const int DefaultSortColumn = 6; // Total Duration

    private readonly IWorkspaceStatsAggregator _aggregator;
    private readonly IRoslynEtwListener _listener;
    private readonly IAnsiConsole _console;
    private readonly IKeyboardInput _keyboard;
    private readonly IEnvironmentContext _environment;

    public WorkspaceCommandHandler(
        IWorkspaceStatsAggregator aggregator,
        IRoslynEtwListener listener,
        IAnsiConsole console,
        IKeyboardInput keyboard,
        IEnvironmentContext environment)
    {
        _aggregator = aggregator;
        _listener = listener;
        _console = console;
        _keyboard = keyboard;
        _environment = environment;
    }

    /// <summary>
    /// Runs the workspace operations monitoring loop.
    /// </summary>
    /// <returns>Exit code: 0 on success, 1 on error.</returns>
    public async Task<int> ExecuteAsync(int top, CancellationToken cancellationToken)
    {
        if (!_environment.IsRunningAsAdministrator)
        {
            _console.MarkupLine(
                "[red bold]Error:[/] This tool requires [bold]administrator privileges[/] to capture ETW events.");
            _console.MarkupLine("[dim]Please re-run from an elevated terminal.[/]");
            return 1;
        }

        _listener.Start();

        var sortColumn = DefaultSortColumn;
        var ascending = false;

        _console.Clear();

        var initialTable = WorkspaceTableBuilder.Build([], sortColumn, ascending, top);

        await _console.Live(initialTable)
            .AutoClear(true)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ProcessKeyboardInput(ref sortColumn, ref ascending);

                    var snapshot = _aggregator.GetSnapshot();
                    var table = WorkspaceTableBuilder.Build(snapshot, sortColumn, ascending, top);
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

    private void ProcessKeyboardInput(ref int sortColumn, ref bool ascending)
    {
        while (_keyboard.KeyAvailable)
        {
            var key = _keyboard.ReadKey();

            if (key.KeyChar is < '1' or > '6')
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
}
