using System.CommandLine;
using System.CommandLine.Invocation;
using Olstakh.CodeAnalysisMonitor.Commands;

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
    context.ExitCode = await GeneratorCommandHandler.ExecuteAsync(top, ct);
}
