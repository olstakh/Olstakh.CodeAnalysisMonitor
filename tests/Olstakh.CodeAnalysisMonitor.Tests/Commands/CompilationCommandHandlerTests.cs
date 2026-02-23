using Moq;
using Olstakh.CodeAnalysisMonitor.Commands;
using Olstakh.CodeAnalysisMonitor.Etw;
using Olstakh.CodeAnalysisMonitor.Services;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Commands;

public sealed class CompilationCommandHandlerTests
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
    public async Task ExecuteAsync_WithEvents_RendersProjectNamesInTable()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var aggregator = new CompilationStatsAggregator();
        aggregator.RecordStart("MyApp.Core", baseTime);
        aggregator.RecordStop("MyApp.Core", baseTime.AddSeconds(2));
        aggregator.RecordStart("MyApp.Web", baseTime.AddSeconds(5));
        aggregator.RecordStop("MyApp.Web", baseTime.AddSeconds(8));

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

        var listener = new Mock<ICompilationEtwListener>(MockBehavior.Strict);
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
        Assert.Contains("MyApp.Core", console.Output, StringComparison.Ordinal);
        Assert.Contains("MyApp.Web", console.Output, StringComparison.Ordinal);
        listener.Verify();
        environment.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_WithTopLimit_OnlyShowsTopNProjects()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var aggregator = new CompilationStatsAggregator();
        aggregator.RecordStart("Small", baseTime);
        aggregator.RecordStop("Small", baseTime.AddMilliseconds(100));
        aggregator.RecordStart("Big", baseTime.AddSeconds(1));
        aggregator.RecordStop("Big", baseTime.AddSeconds(6));
        aggregator.RecordStart("Medium", baseTime.AddSeconds(10));
        aggregator.RecordStop("Medium", baseTime.AddSeconds(11));

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

        var listener = new Mock<ICompilationEtwListener>(MockBehavior.Strict);
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

        // "Big" has the highest total (5s), so it should be present
        Assert.Contains("Big", console.Output, StringComparison.Ordinal);
        // Others should be excluded
        Assert.DoesNotContain("Small", console.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("Medium", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteAsync_KeyboardSortInput_ChangesSortOrder()
    {
        using var console = new TestConsole();
        console.EmitAnsiSequences = false;

        var aggregator = CreateFrequentVsSlowAggregator();

        using var cts = new CancellationTokenSource();

        // Simulate pressing '2' (sort by count) then no more keys
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

        var listener = new Mock<ICompilationEtwListener>(MockBehavior.Strict);
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

        // Sorted by count desc: Frequent (3) should appear before Slow (1)
        var frequentIndex = console.Output.IndexOf("Frequent", StringComparison.Ordinal);
        var slowIndex = console.Output.IndexOf("Slow", StringComparison.Ordinal);
        Assert.True(frequentIndex < slowIndex, "Frequent should appear before Slow when sorted by count descending");
    }

    private static CompilationCommandHandler CreateHandler(
        ICompilationStatsAggregator? aggregator = null,
        ICompilationEtwListener? listener = null,
        IAnsiConsole? console = null,
        IKeyboardInput? keyboard = null,
        IEnvironmentContext? environment = null)
    {
        return new CompilationCommandHandler(
            aggregator ?? new CompilationStatsAggregator(),
            listener ?? Mock.Of<ICompilationEtwListener>(),
            console ?? throw new ArgumentNullException(nameof(console)),
            keyboard ?? Mock.Of<IKeyboardInput>(),
            environment ?? Mock.Of<IEnvironmentContext>(e => e.IsRunningAsAdministrator == true));
    }

    /// <summary>
    /// Creates an aggregator with "Frequent" (3 compilations, 50ms each) and "Slow" (1 compilation, 10s).
    /// </summary>
    private static CompilationStatsAggregator CreateFrequentVsSlowAggregator()
    {
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var aggregator = new CompilationStatsAggregator();

        aggregator.RecordStart("Frequent", baseTime);
        aggregator.RecordStop("Frequent", baseTime.AddMilliseconds(50));
        aggregator.RecordStart("Frequent", baseTime.AddSeconds(1));
        aggregator.RecordStop("Frequent", baseTime.AddSeconds(1).AddMilliseconds(50));
        aggregator.RecordStart("Frequent", baseTime.AddSeconds(2));
        aggregator.RecordStop("Frequent", baseTime.AddSeconds(2).AddMilliseconds(50));

        aggregator.RecordStart("Slow", baseTime.AddSeconds(5));
        aggregator.RecordStop("Slow", baseTime.AddSeconds(15));

        return aggregator;
    }
}
