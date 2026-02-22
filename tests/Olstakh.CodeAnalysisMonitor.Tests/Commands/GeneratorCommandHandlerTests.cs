using Moq;
using Olstakh.CodeAnalysisMonitor.Commands;
using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Commands;

public sealed class GeneratorCommandHandlerTests
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
    public async Task ExecuteAsync_WithEvents_RendersGeneratorNamesInTable()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var aggregator = new GeneratorStatsAggregator();
        aggregator.RecordInvocation("MyApp.Generators.FastGen", 5000);
        aggregator.RecordInvocation("MyApp.Generators.SlowGen", 50000);
        aggregator.RecordInvocation("MyApp.Generators.SlowGen", 70000);

        using var cts = new CancellationTokenSource();

        // Keyboard: no keys pressed, then signal cancellation after first refresh
        var keyCallCount = 0;
        var keyboard = new Mock<IKeyboardInput>(MockBehavior.Strict);
        keyboard.Setup(k => k.KeyAvailable)
            .Returns(() =>
            {
                if (keyCallCount++ > 0)
                {
                    // Cancel after the first render cycle
                    cts.Cancel();
                }

                return false;
            })
            .Verifiable();

        var listener = new Mock<ICodeAnalysisEtwListener>(MockBehavior.Strict);
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
        Assert.Contains("FastGen", console.Output, StringComparison.Ordinal);
        Assert.Contains("SlowGen", console.Output, StringComparison.Ordinal);
        listener.Verify();
        environment.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_WithTopLimit_OnlyShowsTopNGenerators()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var aggregator = new GeneratorStatsAggregator();
        aggregator.RecordInvocation("Gen.Alpha", 1000);
        aggregator.RecordInvocation("Gen.Beta", 5000);
        aggregator.RecordInvocation("Gen.Gamma", 100);

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

        var listener = new Mock<ICodeAnalysisEtwListener>(MockBehavior.Strict);
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

        // Gen.Beta has the highest total (5000 ticks), so it should be present
        Assert.Contains("Gen.Beta", console.Output, StringComparison.Ordinal);
        // Gen.Alpha and Gen.Gamma should be excluded
        Assert.DoesNotContain("Gen.Alpha", console.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Gen.Gamma", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_KeyboardSortInput_ChangesSortOrder()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var aggregator = new GeneratorStatsAggregator();
        // Gen.Many has more invocations but less total time
        aggregator.RecordInvocation("Gen.Many", 100);
        aggregator.RecordInvocation("Gen.Many", 100);
        aggregator.RecordInvocation("Gen.Many", 100);
        // Gen.Slow has fewer invocations but more total time
        aggregator.RecordInvocation("Gen.Slow", 9000);

        using var cts = new CancellationTokenSource();

        // Simulate pressing '2' (sort by invocation count) then no more keys
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

        var listener = new Mock<ICodeAnalysisEtwListener>(MockBehavior.Strict);
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

        // Sorted by invocation count desc: Gen.Many (3) should appear before Gen.Slow (1)
        var manyIndex = console.Output.IndexOf("Gen.Many", StringComparison.Ordinal);
        var slowIndex = console.Output.IndexOf("Gen.Slow", StringComparison.Ordinal);
        Assert.True(manyIndex < slowIndex, "Gen.Many should appear before Gen.Slow when sorted by invocation count descending");
    }

    private static GeneratorCommandHandler CreateHandler(
        IGeneratorStatsAggregator? aggregator = null,
        ICodeAnalysisEtwListener? listener = null,
        IAnsiConsole? console = null,
        IKeyboardInput? keyboard = null,
        IEnvironmentContext? environment = null)
    {
        return new GeneratorCommandHandler(
            aggregator ?? new GeneratorStatsAggregator(),
            listener ?? Mock.Of<ICodeAnalysisEtwListener>(),
            console ?? throw new ArgumentNullException(nameof(console)),
            keyboard ?? Mock.Of<IKeyboardInput>(),
            environment ?? Mock.Of<IEnvironmentContext>(e => e.IsRunningAsAdministrator == true));
    }
}
