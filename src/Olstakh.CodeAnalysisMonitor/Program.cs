using System.CommandLine;
using System.Text;
using Olstakh.CodeAnalysisMonitor.Commands;
using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

if (!OperatingSystem.IsWindows())
{
    await Console.Error.WriteLineAsync("This tool requires Windows (ETW is a Windows-only technology).");
    return 1;
}

var topOption = new Option<int>("--top") { Description = "Maximum number of generators to display", DefaultValueFactory = _ => 50, Recursive = true };

var generatorCommand = new Command("generator", "Monitor source generator performance");
generatorCommand.SetAction(HandleGeneratorCommand);

var workspaceCommand = new Command("workspace", "Monitor Roslyn workspace operations (diagnostics, code fixes, formatting, etc.)");
workspaceCommand.SetAction(HandleWorkspaceCommand);

var rootCommand = new RootCommand(
    "Code Analysis Monitor - monitors Roslyn Code Analysis ETW events " +
    "(source generators, analyzers, workspace operations, etc.) from Visual Studio in real-time.")
{
    topOption,
    generatorCommand,
    workspaceCommand,
};

// Default behavior: when no subcommand is specified, run the generator monitor
rootCommand.SetAction(HandleGeneratorCommand);

return await rootCommand.Parse(args).InvokeAsync();

async Task<int> HandleGeneratorCommand(ParseResult parseResult, CancellationToken ct)
{
    if (!OperatingSystem.IsWindows())
    {
        return 1;
    }

    var top = parseResult.GetValue(topOption) is > 0 and var t ? t : 50;

    var aggregator = new GeneratorStatsAggregator();
    await using var listener = new CodeAnalysisEtwListener(aggregator);

    var handler = new GeneratorCommandHandler(
        aggregator,
        listener,
        AnsiConsole.Console,
        new ConsoleKeyboardInput(),
        new WindowsEnvironmentContext());

    return await handler.ExecuteAsync(top, ct);
}

async Task<int> HandleWorkspaceCommand(ParseResult parseResult, CancellationToken ct)
{
    if (!OperatingSystem.IsWindows())
    {
        return 1;
    }

    var top = parseResult.GetValue(topOption) is > 0 and var t ? t : 50;

    var aggregator = new WorkspaceStatsAggregator();
    await using var listener = new RoslynEtwListener(aggregator);

    var handler = new WorkspaceCommandHandler(
        aggregator,
        listener,
        AnsiConsole.Console,
        new ConsoleKeyboardInput(),
        new WindowsEnvironmentContext());

    return await handler.ExecuteAsync(top, ct);
}
