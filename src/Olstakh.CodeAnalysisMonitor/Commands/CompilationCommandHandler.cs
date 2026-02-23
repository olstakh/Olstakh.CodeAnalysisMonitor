using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Export;
using Olstakh.CodeAnalysisMonitor.Rendering;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;

namespace Olstakh.CodeAnalysisMonitor.Commands;

/// <summary>
/// Handles the "compilation" command: captures ETW events and renders a live server compilation table.
/// </summary>
internal sealed class CompilationCommandHandler
{
    private const int RefreshIntervalMs = 1000;
    private const int DefaultSortColumn = 5; // Total Duration

    private readonly ICompilationStatsAggregator _aggregator;
    private readonly ICompilationEtwListener _listener;
    private readonly IAnsiConsole _console;
    private readonly IKeyboardInput _keyboard;
    private readonly IEnvironmentContext _environment;

    public CompilationCommandHandler(
        ICompilationStatsAggregator aggregator,
        ICompilationEtwListener listener,
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
    /// Runs the compilation monitoring loop.
    /// </summary>
    /// <returns>Exit code: 0 on success, 1 on error.</returns>
    public async Task<int> ExecuteAsync(int top, bool saveOnExit, CancellationToken cancellationToken)
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

        var initialTable = CompilationTableBuilder.Build([], sortColumn, ascending, top);

        await _console.Live(initialTable)
            .AutoClear(true)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ProcessKeyboardInput(ref sortColumn, ref ascending);

                    var snapshot = _aggregator.GetSnapshot()
                        .Where(static s => !string.IsNullOrWhiteSpace(s.Name))
                        .ToList();
                    var table = CompilationTableBuilder.Build(snapshot, sortColumn, ascending, top);
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

        if (saveOnExit)
        {
            SaveCsvFiles();
        }

        return 0;
    }

    private void SaveCsvFiles()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);

        var summaryPath = $"compilation-summary-{timestamp}.csv";
        using (var writer = new StreamWriter(summaryPath))
        {
            CompilationCsvExporter.WriteSummary(writer, _aggregator.GetSnapshot());
        }

        var detailPath = $"compilation-detail-{timestamp}.csv";
        using (var writer = new StreamWriter(detailPath))
        {
            CompilationCsvExporter.WriteDetails(writer, _aggregator.GetDetailedEvents());
        }

        _console.MarkupLine($"[green]Saved:[/] {Markup.Escape(summaryPath)}");
        _console.MarkupLine($"[green]Saved:[/] {Markup.Escape(detailPath)}");
    }

    private void ProcessKeyboardInput(ref int sortColumn, ref bool ascending)
    {
        while (_keyboard.KeyAvailable)
        {
            var key = _keyboard.ReadKey();

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
}
