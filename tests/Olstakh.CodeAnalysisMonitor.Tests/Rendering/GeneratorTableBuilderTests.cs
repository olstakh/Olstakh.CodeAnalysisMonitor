using Olstakh.CodeAnalysisMonitor.Models;
using Olstakh.CodeAnalysisMonitor.Rendering;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Rendering;

public sealed class GeneratorTableBuilderTests
{
    private static readonly IReadOnlyList<GeneratorStats> SampleStats =
    [
        new()
        {
            Name = "GenAlpha",
            InvocationCount = 10,
            AverageDuration = TimeSpan.FromMilliseconds(50),
            TotalDuration = TimeSpan.FromMilliseconds(500),
            P90Duration = TimeSpan.FromMilliseconds(80),
        },
        new()
        {
            Name = "GenBeta",
            InvocationCount = 5,
            AverageDuration = TimeSpan.FromMilliseconds(200),
            TotalDuration = TimeSpan.FromMilliseconds(1000),
            P90Duration = TimeSpan.FromMilliseconds(300),
        },
        new()
        {
            Name = "GenGamma",
            InvocationCount = 20,
            AverageDuration = TimeSpan.FromMilliseconds(10),
            TotalDuration = TimeSpan.FromMilliseconds(200),
            P90Duration = TimeSpan.FromMilliseconds(15),
        },
    ];

    [Fact]
    public void Build_WithEmptyStats_ReturnsTableWithoutError()
    {
        var result = GeneratorTableBuilder.Build([], sortColumn: 5, ascending: false, maxRows: 50);

        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithStats_ReturnsTableWithoutError()
    {
        var result = GeneratorTableBuilder.Build(SampleStats, sortColumn: 5, ascending: false, maxRows: 50);

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
            () => GeneratorTableBuilder.Build(SampleStats, sortColumn, ascending: false, maxRows: 50));

        Assert.Null(exception);
    }

    [Fact]
    public void Build_WithMaxRows_LimitsOutput()
    {
        // We can verify indirectly: build with maxRows=1, render to string, and check only one data row name appears
        var rendered = RenderToString(
            GeneratorTableBuilder.Build(SampleStats, sortColumn: 5, ascending: false, maxRows: 1));

        // Sorted by Total Duration desc: GenBeta (1000ms) should be the only row
        Assert.Contains("GenBeta", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("GenAlpha", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("GenGamma", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SortByTotalDurationDescending_MostExpensiveFirst()
    {
        var rendered = RenderToString(
            GeneratorTableBuilder.Build(SampleStats, sortColumn: 5, ascending: false, maxRows: 50));

        var betaIndex = rendered.IndexOf("GenBeta", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("GenAlpha", StringComparison.Ordinal);
        var gammaIndex = rendered.IndexOf("GenGamma", StringComparison.Ordinal);

        // GenBeta (1000ms) > GenAlpha (500ms) > GenGamma (200ms)
        Assert.True(betaIndex < alphaIndex, "GenBeta should appear before GenAlpha");
        Assert.True(alphaIndex < gammaIndex, "GenAlpha should appear before GenGamma");
    }

    [Fact]
    public void Build_SortByTotalDurationAscending_LeastExpensiveFirst()
    {
        var rendered = RenderToString(
            GeneratorTableBuilder.Build(SampleStats, sortColumn: 5, ascending: true, maxRows: 50));

        var betaIndex = rendered.IndexOf("GenBeta", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("GenAlpha", StringComparison.Ordinal);
        var gammaIndex = rendered.IndexOf("GenGamma", StringComparison.Ordinal);

        // GenGamma (200ms) < GenAlpha (500ms) < GenBeta (1000ms)
        Assert.True(gammaIndex < alphaIndex, "GenGamma should appear before GenAlpha");
        Assert.True(alphaIndex < betaIndex, "GenAlpha should appear before GenBeta");
    }

    [Fact]
    public void Build_SortByNameDescending_AlphabeticalReverse()
    {
        var rendered = RenderToString(
            GeneratorTableBuilder.Build(SampleStats, sortColumn: 1, ascending: false, maxRows: 50));

        var gammaIndex = rendered.IndexOf("GenGamma", StringComparison.Ordinal);
        var betaIndex = rendered.IndexOf("GenBeta", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("GenAlpha", StringComparison.Ordinal);

        Assert.True(gammaIndex < betaIndex, "GenGamma should appear before GenBeta");
        Assert.True(betaIndex < alphaIndex, "GenBeta should appear before GenAlpha");
    }

    [Fact]
    public void Build_SortByInvocationCountDescending_HighestFirst()
    {
        var rendered = RenderToString(
            GeneratorTableBuilder.Build(SampleStats, sortColumn: 2, ascending: false, maxRows: 50));

        var gammaIndex = rendered.IndexOf("GenGamma", StringComparison.Ordinal);
        var alphaIndex = rendered.IndexOf("GenAlpha", StringComparison.Ordinal);
        var betaIndex = rendered.IndexOf("GenBeta", StringComparison.Ordinal);

        // GenGamma (20) > GenAlpha (10) > GenBeta (5)
        Assert.True(gammaIndex < alphaIndex, "GenGamma should appear before GenAlpha");
        Assert.True(alphaIndex < betaIndex, "GenAlpha should appear before GenBeta");
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
