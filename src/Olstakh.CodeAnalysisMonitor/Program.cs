using System.CommandLine;
using System.CommandLine.Invocation;
using Olstakh.CodeAnalysisMonitor.Commands;
using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;

var topOption = new Option<int>("--top", () => 50, "Maximum number of generators to display");

var generatorCommand = new Command("generator", "Monitor source generator performance");
generatorCommand.SetHandler(HandleGeneratorCommand);

var rootCommand = new RootCommand(
    "Code Analysis Monitor - monitors Roslyn Code Analysis ETW events " +
    "(source generators, analyzers, etc.) from Visual Studio in real-time.")
{
    generatorCommand,
};

// Default behavior: when no subcommand is specified, run the generator monitor
rootCommand.AddGlobalOption(topOption);
rootCommand.SetHandler(HandleGeneratorCommand);

return await rootCommand.InvokeAsync(args);

async Task HandleGeneratorCommand(InvocationContext context)
{
    var top = context.ParseResult.GetValueForOption(topOption) is > 0 and var t ? t : 50;
    var ct = context.GetCancellationToken();

    var aggregator = new GeneratorStatsAggregator();
    await using var listener = new CodeAnalysisEtwListener(aggregator);

    var handler = new GeneratorCommandHandler(
        aggregator,
        listener,
        AnsiConsole.Console,
        new ConsoleKeyboardInput(),
        new WindowsEnvironmentContext());

    context.ExitCode = await handler.ExecuteAsync(top, ct);
}
