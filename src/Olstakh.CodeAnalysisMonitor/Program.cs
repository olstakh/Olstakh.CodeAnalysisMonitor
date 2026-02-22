using System.CommandLine;
using Olstakh.CodeAnalysisMonitor.Commands;
using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;

var topOption = new Option<int>("--top", "Maximum number of generators to display") { DefaultValueFactory = _ => 50, Recursive = true };

var generatorCommand = new Command("generator", "Monitor source generator performance");
generatorCommand.SetAction(HandleGeneratorCommand);

var rootCommand = new RootCommand(
    "Code Analysis Monitor - monitors Roslyn Code Analysis ETW events " +
    "(source generators, analyzers, etc.) from Visual Studio in real-time.")
{
    topOption,
    generatorCommand,
};

// Default behavior: when no subcommand is specified, run the generator monitor
rootCommand.SetAction(HandleGeneratorCommand);

return await rootCommand.Parse(args).InvokeAsync();

async Task<int> HandleGeneratorCommand(ParseResult parseResult, CancellationToken ct)
{
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
