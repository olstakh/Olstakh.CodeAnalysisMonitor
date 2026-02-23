using Olstakh.CodeAnalysisMonitor.Services;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Services;

public sealed class WorkspaceStatsAggregatorTests
{
    private readonly WorkspaceStatsAggregator _sut = new();

    [Fact]
    public void GetSnapshot_WhenNoEvents_ReturnsEmptyList()
    {
        var result = _sut.GetSnapshot();

        Assert.Empty(result);
    }

    [Fact]
    public void GetSnapshot_AfterSingleCompletedBlock_ReturnsCorrectStats()
    {
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 100);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal("60", stat.OperationName); // No definitions registered yet
        Assert.Equal(1, stat.CompletedCount);
        Assert.Equal(0, stat.CanceledCount);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stat.AverageDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stat.TotalDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stat.P90Duration);
    }

    [Fact]
    public void GetSnapshot_AfterSingleCanceledBlock_ReturnsCorrectStats()
    {
        _sut.RecordBlockCanceled(functionId: 60, durationMs: 50);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(0, stat.CompletedCount);
        Assert.Equal(1, stat.CanceledCount);
        Assert.Equal(TimeSpan.FromMilliseconds(50), stat.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_WithMultipleBlocks_ComputesCorrectAverage()
    {
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 100);
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 300);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(2, stat.CompletedCount);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stat.AverageDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(400), stat.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_WithTenBlocks_ComputesP90AsNinthHighest()
    {
        // 10 values: 10, 20, ..., 100. P90 index = ceil(10*0.9)-1 = 8 → sorted[8] = 90
        for (var i = 1; i <= 10; i++)
        {
            _sut.RecordBlockCompleted(functionId: 60, durationMs: i * 10);
        }

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(TimeSpan.FromMilliseconds(90), stat.P90Duration);
    }

    [Fact]
    public void GetSnapshot_MixesCompletedAndCanceled_TracksCountsSeparately()
    {
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 100);
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 200);
        _sut.RecordBlockCanceled(functionId: 60, durationMs: 50);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(2, stat.CompletedCount);
        Assert.Equal(1, stat.CanceledCount);
        // Total duration includes all blocks (completed + canceled)
        Assert.Equal(TimeSpan.FromMilliseconds(350), stat.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_TracksMultipleFunctionIdsSeparately()
    {
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 100);
        _sut.RecordBlockCompleted(functionId: 76, durationMs: 200);
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 300);

        var result = _sut.GetSnapshot();

        Assert.Equal(2, result.Count);

        var op60 = Assert.Single(result, s => string.Equals(s.OperationName, "60", StringComparison.Ordinal));
        Assert.Equal(2, op60.CompletedCount);
        Assert.Equal(TimeSpan.FromMilliseconds(400), op60.TotalDuration);

        var op76 = Assert.Single(result, s => string.Equals(s.OperationName, "76", StringComparison.Ordinal));
        Assert.Equal(1, op76.CompletedCount);
        Assert.Equal(TimeSpan.FromMilliseconds(200), op76.TotalDuration);
    }

    [Fact]
    public void RegisterFunctionDefinitions_ResolvesNamesInSnapshot()
    {
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 100);
        _sut.RecordBlockCompleted(functionId: 76, durationMs: 200);

        _sut.RegisterFunctionDefinitions(
            """
            1.0.0
            60 Workspace_Project_GetCompilation Undefined
            76 FindReference Undefined
            """);

        var result = _sut.GetSnapshot();

        Assert.Contains(result, s => string.Equals(s.OperationName, "Workspace_Project_GetCompilation", StringComparison.Ordinal));
        Assert.Contains(result, s => string.Equals(s.OperationName, "FindReference", StringComparison.Ordinal));
    }

    [Fact]
    public void RegisterFunctionDefinitions_IgnoresInvalidLines()
    {
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 100);

        _sut.RegisterFunctionDefinitions(
            """
            1.0.0
            not-a-number SomeName Undefined
            60 Workspace_Project_GetCompilation Undefined
            
            """);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal("Workspace_Project_GetCompilation", stat.OperationName);
    }

    [Fact]
    public void GetSnapshot_IsImmutableSnapshot_NotAffectedBySubsequentRecords()
    {
        _sut.RecordBlockCompleted(functionId: 60, durationMs: 100);

        var snapshot = _sut.GetSnapshot();

        _sut.RecordBlockCompleted(functionId: 60, durationMs: 9000);

        var stat = Assert.Single(snapshot);
        Assert.Equal(1, stat.CompletedCount);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stat.TotalDuration);
    }

    [Fact]
    public async Task RecordBlockCompleted_ConcurrentWrites_DoNotLoseData()
    {
        const int threadCount = 10;
        const int blocksPerThread = 1000;

        var tasks = Enumerable.Range(0, threadCount).Select(_ =>
            Task.Run(() =>
            {
                for (var i = 0; i < blocksPerThread; i++)
                {
                    _sut.RecordBlockCompleted(functionId: 60, durationMs: 10);
                }
            }, TestContext.Current.CancellationToken));

        await Task.WhenAll(tasks);

        var result = _sut.GetSnapshot();
        var stat = Assert.Single(result);
        Assert.Equal(threadCount * blocksPerThread, stat.CompletedCount);
        Assert.Equal(TimeSpan.FromMilliseconds(threadCount * blocksPerThread * 10), stat.TotalDuration);
    }
}
