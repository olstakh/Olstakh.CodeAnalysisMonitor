using Moq;
using Olstakh.CodeAnalysisMonitor.Commands;
using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Commands;

public sealed class WorkspaceCommandHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNotAdmin_ReturnsExitCode1AndShowsError()
    {
        using var console = new TestConsole();
        var environment = new Mock<IEnvironmentContext>(MockBehavior.Strict);
        environment.Setup(e => e.IsRunningAsAdministrator).Returns(false).Verifiable();

        var handler = CreateHandler(
            console: console,
            environment: environment.Object);

        var exitCode = await handler.ExecuteAsync(top: 50, TestContext.Current.CancellationToken);

        Assert.Equal(1, exitCode);
        Assert.Contains("administrator privileges", console.Output, StringComparison.Ordinal);
        environment.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_WithEvents_RendersOperationNamesInTable()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var aggregator = new WorkspaceStatsAggregator();
        aggregator.RecordBlockCompleted(functionId: 60, durationMs: 100);
        aggregator.RecordBlockCompleted(functionId: 76, durationMs: 200);
        aggregator.RecordBlockCanceled(functionId: 76, durationMs: 50);

        aggregator.RegisterFunctionDefinitions(
            """
            1.0.0
            60 Workspace_Project_GetCompilation Undefined
            76 FindReference Undefined
            """);

        using var cts = new CancellationTokenSource();

        var keyCallCount = 0;
        var keyboard = new Mock<IKeyboardInput>(MockBehavior.Strict);
        keyboard.Setup(k => k.KeyAvailable)
            .Returns(() =>
            {
                if (keyCallCount++ > 0)
                {
                    cts.Cancel();
                }

                return false;
            })
            .Verifiable();

        var listener = new Mock<IRoslynEtwListener>(MockBehavior.Strict);
        listener.Setup(l => l.Start()).Verifiable();

        var environment = new Mock<IEnvironmentContext>(MockBehavior.Strict);
        environment.Setup(e => e.IsRunningAsAdministrator).Returns(true).Verifiable();

        var handler = CreateHandler(
            aggregator: aggregator,
            listener: listener.Object,
            console: console,
            keyboard: keyboard.Object,
            environment: environment.Object);

        var exitCode = await handler.ExecuteAsync(top: 50, cts.Token);

        Assert.Equal(0, exitCode);
        Assert.Contains("Workspace_Project_GetCompilation", console.Output, StringComparison.Ordinal);
        Assert.Contains("FindReference", console.Output, StringComparison.Ordinal);
        listener.Verify();
        environment.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_WithTopLimit_OnlyShowsTopNOperations()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var aggregator = new WorkspaceStatsAggregator();
        aggregator.RecordBlockCompleted(functionId: 60, durationMs: 100);
        aggregator.RecordBlockCompleted(functionId: 76, durationMs: 500);
        aggregator.RecordBlockCompleted(functionId: 13, durationMs: 10);

        using var cts = new CancellationTokenSource();

        var keyCallCount = 0;
        var keyboard = new Mock<IKeyboardInput>(MockBehavior.Strict);
        keyboard.Setup(k => k.KeyAvailable)
            .Returns(() =>
            {
                if (keyCallCount++ > 0)
                {
                    cts.Cancel();
                }

                return false;
            })
            .Verifiable();

        var listener = new Mock<IRoslynEtwListener>(MockBehavior.Strict);
        listener.Setup(l => l.Start()).Verifiable();

        var environment = new Mock<IEnvironmentContext>(MockBehavior.Strict);
        environment.Setup(e => e.IsRunningAsAdministrator).Returns(true).Verifiable();

        var handler = CreateHandler(
            aggregator: aggregator,
            listener: listener.Object,
            console: console,
            keyboard: keyboard.Object,
            environment: environment.Object);

        // top=1 → only the highest total duration should appear
        var exitCode = await handler.ExecuteAsync(top: 1, cts.Token);

        Assert.Equal(0, exitCode);

        // FunctionId 76 has the highest total (500ms), so it should be present
        Assert.Contains("76", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_KeyboardSortInput_ChangesSortOrder()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var aggregator = new WorkspaceStatsAggregator();
        // FunctionId 60: many completions, low total
        aggregator.RecordBlockCompleted(functionId: 60, durationMs: 10);
        aggregator.RecordBlockCompleted(functionId: 60, durationMs: 10);
        aggregator.RecordBlockCompleted(functionId: 60, durationMs: 10);
        // FunctionId 76: fewer completions, high total
        aggregator.RecordBlockCompleted(functionId: 76, durationMs: 900);

        using var cts = new CancellationTokenSource();

        // Simulate pressing '2' (sort by completed count) then no more keys
        var keyPresses = new Queue<ConsoleKeyInfo>(
        [
            new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false),
        ]);

        var keyboard = new Mock<IKeyboardInput>(MockBehavior.Strict);
        keyboard.Setup(k => k.KeyAvailable)
            .Returns(() =>
            {
                if (keyPresses.Count > 0)
                {
                    return true;
                }

                cts.Cancel();
                return false;
            })
            .Verifiable();
        keyboard.Setup(k => k.ReadKey())
            .Returns(keyPresses.Dequeue)
            .Verifiable();

        var listener = new Mock<IRoslynEtwListener>(MockBehavior.Strict);
        listener.Setup(l => l.Start()).Verifiable();

        var environment = new Mock<IEnvironmentContext>(MockBehavior.Strict);
        environment.Setup(e => e.IsRunningAsAdministrator).Returns(true).Verifiable();

        var handler = CreateHandler(
            aggregator: aggregator,
            listener: listener.Object,
            console: console,
            keyboard: keyboard.Object,
            environment: environment.Object);

        var exitCode = await handler.ExecuteAsync(top: 50, cts.Token);

        Assert.Equal(0, exitCode);

        // Sorted by completed count desc: FunctionId 60 (3) should appear before 76 (1)
        var id60Index = console.Output.IndexOf("60", StringComparison.Ordinal);
        var id76Index = console.Output.IndexOf("76", StringComparison.Ordinal);
        Assert.True(id60Index < id76Index, "FunctionId 60 should appear before 76 when sorted by completed count descending");
    }

    private static WorkspaceCommandHandler CreateHandler(
        IWorkspaceStatsAggregator? aggregator = null,
        IRoslynEtwListener? listener = null,
        IAnsiConsole? console = null,
        IKeyboardInput? keyboard = null,
        IEnvironmentContext? environment = null)
    {
        return new WorkspaceCommandHandler(
            aggregator ?? new WorkspaceStatsAggregator(),
            listener ?? Mock.Of<IRoslynEtwListener>(),
            console ?? throw new ArgumentNullException(nameof(console)),
            keyboard ?? Mock.Of<IKeyboardInput>(),
            environment ?? Mock.Of<IEnvironmentContext>(e => e.IsRunningAsAdministrator == true));
    }
}
