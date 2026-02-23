using Olstakh.CodeAnalysisMonitor.Services;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Services;

public sealed class CompilationStatsAggregatorTests
{
    private readonly CompilationStatsAggregator _sut = new();

    [Fact]
    public void GetSnapshot_WhenNoEvents_ReturnsEmptyList()
    {
        var result = _sut.GetSnapshot();

        Assert.Empty(result);
    }

    [Fact]
    public void GetSnapshot_WithStartOnly_ReturnsEmptyList()
    {
        _sut.RecordStart("MyProject", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        var result = _sut.GetSnapshot();

        Assert.Empty(result);
    }

    [Fact]
    public void GetSnapshot_WithStopOnly_ReturnsEmptyList()
    {
        // Stop without a matching start should be ignored
        _sut.RecordStop("MyProject", new DateTime(2026, 1, 1, 12, 0, 5, DateTimeKind.Utc));

        var result = _sut.GetSnapshot();

        Assert.Empty(result);
    }

    [Fact]
    public void GetSnapshot_AfterSingleCompletedCompilation_ReturnsCorrectStats()
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var stop = start.AddSeconds(2);

        _sut.RecordStart("MyProject", start);
        _sut.RecordStop("MyProject", stop);

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal("MyProject", stat.Name);
        Assert.Equal(1, stat.CompilationCount);
        Assert.Equal(TimeSpan.FromSeconds(2), stat.AverageDuration);
        Assert.Equal(TimeSpan.FromSeconds(2), stat.TotalDuration);
        Assert.Equal(TimeSpan.FromSeconds(2), stat.P90Duration);
    }

    [Fact]
    public void GetSnapshot_WithMultipleCompilations_ComputesCorrectAverage()
    {
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        _sut.RecordStart("MyProject", baseTime);
        _sut.RecordStop("MyProject", baseTime.AddSeconds(1));

        _sut.RecordStart("MyProject", baseTime.AddSeconds(5));
        _sut.RecordStop("MyProject", baseTime.AddSeconds(8));

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(2, stat.CompilationCount);
        Assert.Equal(TimeSpan.FromSeconds(2), stat.AverageDuration); // (1s + 3s) / 2 = 2s
        Assert.Equal(TimeSpan.FromSeconds(4), stat.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_WithTenCompilations_ComputesP90AsNinthHighest()
    {
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // 10 compilations with durations: 100ms, 200ms, ..., 1000ms
        for (var i = 1; i <= 10; i++)
        {
            var start = baseTime.AddSeconds(i * 10);
            _sut.RecordStart("MyProject", start);
            _sut.RecordStop("MyProject", start.AddMilliseconds(i * 100));
        }

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(10, stat.CompilationCount);
        // P90 index = ceil(10*0.9)-1 = 8 → sorted[8] = 900ms
        Assert.Equal(900, stat.P90Duration.TotalMilliseconds, precision: 1);
    }

    [Fact]
    public void GetSnapshot_TracksMultipleProjectsSeparately()
    {
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        _sut.RecordStart("ProjectA", baseTime);
        _sut.RecordStop("ProjectA", baseTime.AddSeconds(1));

        _sut.RecordStart("ProjectB", baseTime.AddSeconds(5));
        _sut.RecordStop("ProjectB", baseTime.AddSeconds(8));

        _sut.RecordStart("ProjectA", baseTime.AddSeconds(10));
        _sut.RecordStop("ProjectA", baseTime.AddSeconds(12));

        var result = _sut.GetSnapshot();

        Assert.Equal(2, result.Count);

        var projA = Assert.Single(result, s => string.Equals(s.Name, "ProjectA", StringComparison.Ordinal));
        Assert.Equal(2, projA.CompilationCount);
        Assert.Equal(TimeSpan.FromSeconds(3), projA.TotalDuration); // 1s + 2s

        var projB = Assert.Single(result, s => string.Equals(s.Name, "ProjectB", StringComparison.Ordinal));
        Assert.Equal(1, projB.CompilationCount);
        Assert.Equal(TimeSpan.FromSeconds(3), projB.TotalDuration);
    }

    [Fact]
    public void GetSnapshot_MatchesProjectNamesCaseInsensitively()
    {
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        _sut.RecordStart("MyProject", baseTime);
        _sut.RecordStop("myproject", baseTime.AddSeconds(1));

        var result = _sut.GetSnapshot();

        var stat = Assert.Single(result);
        Assert.Equal(1, stat.CompilationCount);
    }

    [Fact]
    public void GetSnapshot_IsImmutableSnapshot_NotAffectedBySubsequentRecords()
    {
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        _sut.RecordStart("MyProject", baseTime);
        _sut.RecordStop("MyProject", baseTime.AddSeconds(1));

        var snapshot = _sut.GetSnapshot();

        _sut.RecordStart("MyProject", baseTime.AddSeconds(5));
        _sut.RecordStop("MyProject", baseTime.AddSeconds(15));

        var stat = Assert.Single(snapshot);
        Assert.Equal(1, stat.CompilationCount);
        Assert.Equal(TimeSpan.FromSeconds(1), stat.TotalDuration);
    }

    [Fact]
    public async Task RecordStartAndStop_ConcurrentWrites_DoNotLoseData()
    {
        const int threadCount = 10;
        const int compilationsPerThread = 100;

        var tasks = Enumerable.Range(0, threadCount).Select(threadIndex =>
            Task.Run(() =>
            {
                var projectName = $"ConcurrentProject-{threadIndex}";

                for (var i = 0; i < compilationsPerThread; i++)
                {
                    var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddMilliseconds(i * 100);
                    _sut.RecordStart(projectName, start);
                    _sut.RecordStop(projectName, start.AddMilliseconds(10));
                }
            }, TestContext.Current.CancellationToken));

        await Task.WhenAll(tasks);

        var result = _sut.GetSnapshot();
        Assert.Equal(threadCount, result.Count);

        var totalCompilations = result.Sum(static s => s.CompilationCount);
        Assert.Equal(threadCount * compilationsPerThread, totalCompilations);
    }

    [Fact]
    public void GetDetailedEvents_WhenNoEvents_ReturnsEmptyList()
    {
        var result = _sut.GetDetailedEvents();

        Assert.Empty(result);
    }

    [Fact]
    public void GetDetailedEvents_AfterCompletedCompilation_RecordsEvent()
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var stop = start.AddSeconds(2);

        _sut.RecordStart("MyProject", start);
        _sut.RecordStop("MyProject", stop);

        var result = _sut.GetDetailedEvents();

        var evt = Assert.Single(result);
        Assert.Equal("MyProject", evt.ProjectName);
        Assert.Equal(stop, evt.Timestamp);
        Assert.Equal(2000.0, evt.DurationMs, precision: 1);
    }

    [Fact]
    public void GetDetailedEvents_WithStartOnly_ReturnsEmpty()
    {
        _sut.RecordStart("MyProject", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        var result = _sut.GetDetailedEvents();

        Assert.Empty(result);
    }

    [Fact]
    public void GetDetailedEvents_RecordsMultipleEvents()
    {
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        _sut.RecordStart("ProjectA", baseTime);
        _sut.RecordStop("ProjectA", baseTime.AddSeconds(1));

        _sut.RecordStart("ProjectB", baseTime.AddSeconds(5));
        _sut.RecordStop("ProjectB", baseTime.AddSeconds(8));

        var result = _sut.GetDetailedEvents();

        Assert.Equal(2, result.Count);
    }
}
