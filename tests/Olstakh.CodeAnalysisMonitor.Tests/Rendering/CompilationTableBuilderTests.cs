using Olstakh.CodeAnalysisMonitor.Models;
using Olstakh.CodeAnalysisMonitor.Rendering;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Rendering;

public sealed class CompilationTableBuilderTests
{
    private static readonly IReadOnlyList<CompilationStats> SampleStats =
    [
        new()
        {
            Name = "ProjectAlpha",
            CompilationCount = 10,
            AverageDuration = TimeSpan.FromMilliseconds(500),
            TotalDuration = TimeSpan.FromMilliseconds(5000),
            P90Duration = TimeSpan.FromMilliseconds(800),
        },
        new()
        {
            Name = "ProjectBeta",
            CompilationCount = 5,
            AverageDuration = TimeSpan.FromSeconds(2),
            TotalDuration = TimeSpan.FromSeconds(10),
            P90Duration = TimeSpan.FromSeconds(3),
        },
        new()
        {
            Name = "ProjectGamma",
            CompilationCount = 20,
            AverageDuration = TimeSpan.FromMilliseconds(100),
            TotalDuration = TimeSpan.FromMilliseconds(2000),
            P90Duration = TimeSpan.FromMilliseconds(150),
        },
    ];

    [Fact]
    public void Build_WithEmptyStats_ReturnsTableWithoutError()
    {
        var result = CompilationTableBuilder.Build([], sortColumn: 5, ascending: false, maxRows: 50);

        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithStats_ReturnsTableWithoutError()
    {
        var result = CompilationTableBuilder.Build(SampleStats, sortColumn: 5, ascending: false, maxRows: 50);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Build_WithValidSortColumn_DoesNotThrow(int sortColumn)
    {
        var exception = Record.Exception(
            () => CompilationTableBuilder.Build(SampleStats, sortColumn, ascending: false, maxRows: 50));

        Assert.Null(exception);
    }

    [Fact]
    public void Build_WithMaxRows_LimitsOutput()
    {
        var rendered = RenderToString(
            CompilationTableBuilder.Build(SampleStats, sortColumn: 5, ascending: false, maxRows: 1));

        // Sorted by Total Duration desc: ProjectBeta (10s) should be the only row
        Assert.Contains("ProjectBeta", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectAlpha", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectGamma", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SortByTotalDurationDescending_MostExpensiveFirst()
    {
        var rendered = RenderToString(
            CompilationTableBuilder.Build(SampleStats, sortColumn: 5, ascending: false, maxRows: 50));

        var betaIndex = rendered.IndexOf("ProjectBeta", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("ProjectAlpha", StringComparison.Ordinal);
        var gammaIndex = rendered.IndexOf("ProjectGamma", StringComparison.Ordinal);

        // ProjectBeta (10s) > ProjectAlpha (5s) > ProjectGamma (2s)
        Assert.True(betaIndex < alphaIndex, "ProjectBeta should appear before ProjectAlpha");
        Assert.True(alphaIndex < gammaIndex, "ProjectAlpha should appear before ProjectGamma");
    }

    [Fact]
    public void Build_SortByTotalDurationAscending_LeastExpensiveFirst()
    {
        var rendered = RenderToString(
            CompilationTableBuilder.Build(SampleStats, sortColumn: 5, ascending: true, maxRows: 50));

        var betaIndex = rendered.IndexOf("ProjectBeta", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("ProjectAlpha", StringComparison.Ordinal);
        var gammaIndex = rendered.IndexOf("ProjectGamma", StringComparison.Ordinal);

        Assert.True(gammaIndex < alphaIndex, "ProjectGamma should appear before ProjectAlpha");
        Assert.True(alphaIndex < betaIndex, "ProjectAlpha should appear before ProjectBeta");
    }

    [Fact]
    public void Build_SortByCompilationCountDescending_HighestFirst()
    {
        var rendered = RenderToString(
            CompilationTableBuilder.Build(SampleStats, sortColumn: 2, ascending: false, maxRows: 50));

        var gammaIndex = rendered.IndexOf("ProjectGamma", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("ProjectAlpha", StringComparison.Ordinal);
        var betaIndex = rendered.IndexOf("ProjectBeta", StringComparison.Ordinal);

        // ProjectGamma (20) > ProjectAlpha (10) > ProjectBeta (5)
        Assert.True(gammaIndex < alphaIndex, "ProjectGamma should appear before ProjectAlpha");
        Assert.True(alphaIndex < betaIndex, "ProjectAlpha should appear before ProjectBeta");
    }

    [Fact]
    public void Build_SortByNameDescending_AlphabeticalReverse()
    {
        var rendered = RenderToString(
            CompilationTableBuilder.Build(SampleStats, sortColumn: 1, ascending: false, maxRows: 50));

        var gammaIndex = rendered.IndexOf("ProjectGamma", StringComparison.Ordinal);
        var betaIndex = rendered.IndexOf("ProjectBeta", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("ProjectAlpha", StringComparison.Ordinal);

        Assert.True(gammaIndex < betaIndex, "ProjectGamma should appear before ProjectBeta");
        Assert.True(betaIndex < alphaIndex, "ProjectBeta should appear before ProjectAlpha");
    }

    private static string RenderToString(Spectre.Console.Rendering.IRenderable renderable)
    {
        var writer = new System.IO.StringWriter();
        var console = Spectre.Console.AnsiConsole.Create(new Spectre.Console.AnsiConsoleSettings
        {
            Ansi = Spectre.Console.AnsiSupport.No,
            ColorSystem = Spectre.Console.ColorSystemSupport.NoColors,
            Out = new Spectre.Console.AnsiConsoleOutput(writer),
        });

        console.Write(renderable);
        return writer.ToString();
    }
}
