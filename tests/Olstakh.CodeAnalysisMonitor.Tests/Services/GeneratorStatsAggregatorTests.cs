using Olstakh.CodeAnalysisMonitor.Services;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Services;

public sealed class GeneratorStatsAggregatorTests
{
    private readonly GeneratorStatsAggregator _sut = new();

    [Fact]
    public void GetSnapshot_WhenNoInvocations_ReturnsEmptyList()
    {
        var result = _sut.GetSnapshot();

        Assert.Empty(result);
    }

    [Fact]
    public void GetSnapshot_AfterSingleInvocation_ReturnsCorrectStats()
    {
        _sut.RecordInvocation("Gen.A", 1000);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal("Gen.A", stat.Name);
        Assert.Equal(1, stat.InvocationCount);
        Assert.Equal(TimeSpan.FromTicks(1000), stat.AverageDuration);
        Assert.Equal(TimeSpan.FromTicks(1000), stat.TotalDuration);
        Assert.Equal(TimeSpan.FromTicks(1000), stat.P90Duration);
    }

    [Fact]
    public void GetSnapshot_WithMultipleInvocations_ComputesCorrectAverage()
    {
        _sut.RecordInvocation("Gen.A", 1000);
        _sut.RecordInvocation("Gen.A", 3000);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(2, stat.InvocationCount);
        Assert.Equal(TimeSpan.FromTicks(2000), stat.AverageDuration);
        Assert.Equal(TimeSpan.FromTicks(4000), stat.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_WithMultipleInvocations_ComputesCorrectTotal()
    {
        _sut.RecordInvocation("Gen.A", 100);
        _sut.RecordInvocation("Gen.A", 200);
        _sut.RecordInvocation("Gen.A", 300);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(TimeSpan.FromTicks(600), stat.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_WithTenInvocations_ComputesP90AsNinthHighest()
    {
        // 10 values: 100, 200, ..., 1000. P90 index = ceil(10*0.9)-1 = 8 → sorted[8] = 900
        for (var i = 1; i <= 10; i++)
        {
            _sut.RecordInvocation("Gen.A", i * 100);
        }

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(TimeSpan.FromTicks(900), stat.P90Duration);
    }

    [Fact]
    public void GetSnapshot_TracksMultipleGeneratorsSeparately()
    {
        _sut.RecordInvocation("Gen.A", 1000);
        _sut.RecordInvocation("Gen.B", 2000);
        _sut.RecordInvocation("Gen.A", 3000);

        var result = _sut.GetSnapshot();

        Assert.Equal(2, result.Count);

        var genA = Assert.Single(result, s => string.Equals(s.Name, "Gen.A", StringComparison.Ordinal));
        Assert.Equal(2, genA.InvocationCount);
        Assert.Equal(TimeSpan.FromTicks(4000), genA.TotalDuration);

        var genB = Assert.Single(result, s => string.Equals(s.Name, "Gen.B", StringComparison.Ordinal));
        Assert.Equal(1, genB.InvocationCount);
        Assert.Equal(TimeSpan.FromTicks(2000), genB.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_IsImmutableSnapshot_NotAffectedBySubsequentRecords()
    {
        _sut.RecordInvocation("Gen.A", 1000);

        var snapshot = _sut.GetSnapshot();

        _sut.RecordInvocation("Gen.A", 9000);

        var stat = Assert.Single(snapshot);
        Assert.Equal(1, stat.InvocationCount);
        Assert.Equal(TimeSpan.FromTicks(1000), stat.TotalDuration);
    }

    [Fact]
    public async Task RecordInvocation_ConcurrentWrites_DoNotLoseData()
    {
        const int threadCount = 10;
        const int invocationsPerThread = 1000;

        var tasks = Enumerable.Range(0, threadCount).Select(_ =>
            Task.Run(() =>
            {
                for (var i = 0; i < invocationsPerThread; i++)
                {
                    _sut.RecordInvocation("Gen.Concurrent", 100);
                }
            }));

        await Task.WhenAll(tasks);

        var result = _sut.GetSnapshot();
        var stat = Assert.Single(result);
        Assert.Equal(threadCount * invocationsPerThread, stat.InvocationCount);
    }
}
