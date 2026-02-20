using System.CommandLine;
using System.Security.Principal;
using Microsoft.Extensions.DependencyInjection;
using Olstakh.CodeAnalysisMonitor;

var liveOption = new Option<bool>(
    "--live",
    "Show events in real-time as they occur in the console.");

var filterOption = new Option<string[]>(
    "--filter",
    "Filter events by payload key=value pairs. Multiple filters use AND logic. " +
    "Example: --filter generatorName=MyGenerator")
{
    AllowMultipleArgumentsPerToken = true,
};

filterOption.SetDefaultValue(Array.Empty<string>());

var captureOption = new Option<CaptureEventKind[]>(
    "--capture",
    "Event types to capture. Currently supported: SingleGeneratorRunTime. " +
    "Defaults to all supported events if not specified.")
{
    AllowMultipleArgumentsPerToken = true,
};

captureOption.SetDefaultValue(new[] { CaptureEventKind.SingleGeneratorRunTime });

var rootCommand = new RootCommand(
    "Code Analysis Monitor - monitors Roslyn Code Analysis ETW events " +
    "(source generators, analyzers, etc.) from Visual Studio in real-time.")
{
    liveOption,
    filterOption,
    captureOption,
};

rootCommand.SetHandler(
    async (bool live, string[] filters, CaptureEventKind[] capture) =>
    {
        if (!IsRunningAsAdministrator())
        {
            await Console.Error.WriteLineAsync(
                "Error: This tool requires administrator privileges to create an ETW trace session.");
            await Console.Error.WriteLineAsync(
                "Please run this command from an elevated (Administrator) terminal.");
            return;
        }

        var eventFilter = filters.Length > 0
            ? EventFilter.Parse(filters)
            : EventFilter.None;

        var captureKinds = capture.ToHashSet();

        using var serviceProvider = new ServiceCollection()
            .AddCodeAnalysisMonitor(eventFilter, liveOutput: live)
            .BuildServiceProvider();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Console.WriteLine("Monitoring Roslyn Code Analysis ETW events. Press Ctrl+C to stop...");

        if (live)
        {
            Console.WriteLine("Live mode: events will be printed as they occur.");
        }

        if (filters.Length > 0)
        {
            Console.WriteLine($"Active filters: {string.Join(", ", filters)}");
        }

        Console.WriteLine($"Capturing: {string.Join(", ", capture)}");
        Console.WriteLine();

        using var session = new MonitorSession(
            serviceProvider.GetServices<ICaptureHandler>(),
            captureKinds);
        session.Run(cts.Token);
        session.PrintSummary();
    },
    liveOption,
    filterOption,
    captureOption);

return await rootCommand.InvokeAsync(args);

static bool IsRunningAsAdministrator()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
