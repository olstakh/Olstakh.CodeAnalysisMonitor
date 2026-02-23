using Olstakh.CodeAnalysisMonitor.Models;
using Olstakh.CodeAnalysisMonitor.Rendering;
using Xunit;

namespace Olstakh.CodeAnalysisMonitor.Tests.Rendering;

public sealed class WorkspaceTableBuilderTests
{
    private static readonly IReadOnlyList<WorkspaceOperationStats> SampleStats =
    [
        new()
        {
            OperationName = "Workspace_Project_GetCompilation",
            CompletedCount = 10,
            CanceledCount = 2,
            AverageDuration = TimeSpan.FromMilliseconds(50),
            TotalDuration = TimeSpan.FromMilliseconds(600),
            P90Duration = TimeSpan.FromMilliseconds(80),
        },
        new()
        {
            OperationName = "FindReference",
            CompletedCount = 5,
            CanceledCount = 0,
            AverageDuration = TimeSpan.FromMilliseconds(200),
            TotalDuration = TimeSpan.FromMilliseconds(1000),
            P90Duration = TimeSpan.FromMilliseconds(300),
        },
        new()
        {
            OperationName = "Diagnostics_SemanticDiagnostic",
            CompletedCount = 20,
            CanceledCount = 5,
            AverageDuration = TimeSpan.FromMilliseconds(10),
            TotalDuration = TimeSpan.FromMilliseconds(250),
            P90Duration = TimeSpan.FromMilliseconds(15),
        },
    ];

    [Fact]
    public void Build_WithEmptyStats_ReturnsTableWithoutError()
    {
        var result = WorkspaceTableBuilder.Build([], sortColumn: 6, ascending: false, maxRows: 50);

        Assert.NotNull(result);
    }

    [Fact]
    public void Build_WithStats_ReturnsTableWithoutError()
    {
        var result = WorkspaceTableBuilder.Build(SampleStats, sortColumn: 6, ascending: false, maxRows: 50);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Build_WithValidSortColumn_DoesNotThrow(int sortColumn)
    {
        var exception = Record.Exception(
            () => WorkspaceTableBuilder.Build(SampleStats, sortColumn, ascending: false, maxRows: 50));

        Assert.Null(exception);
    }

    [Fact]
    public void Build_WithMaxRows_LimitsOutput()
    {
        var rendered = RenderToString(
            WorkspaceTableBuilder.Build(SampleStats, sortColumn: 6, ascending: false, maxRows: 1));

        // Sorted by Total Duration desc: FindReference (1000ms) should be the only row
        Assert.Contains("FindReference", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("Workspace_Project_GetCompilation", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("Diagnostics_SemanticDiagnostic", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SortByTotalDurationDescending_MostExpensiveFirst()
    {
        var rendered = RenderToString(
            WorkspaceTableBuilder.Build(SampleStats, sortColumn: 6, ascending: false, maxRows: 50));

        var findRefIndex = rendered.IndexOf("FindReference", StringComparison.Ordinal);
        var compilationIndex = rendered.IndexOf("Workspace_Project_GetCompilation", StringComparison.Ordinal);
        var diagnosticIndex = rendered.IndexOf("Diagnostics_SemanticDiagnostic", StringComparison.Ordinal);

        // FindReference (1000ms) > Workspace_Project_GetCompilation (600ms) > Diagnostics_SemanticDiagnostic (250ms)
        Assert.True(findRefIndex < compilationIndex, "FindReference should appear before Workspace_Project_GetCompilation");
        Assert.True(compilationIndex < diagnosticIndex, "Workspace_Project_GetCompilation should appear before Diagnostics_SemanticDiagnostic");
    }

    [Fact]
    public void Build_SortByTotalDurationAscending_LeastExpensiveFirst()
    {
        var rendered = RenderToString(
            WorkspaceTableBuilder.Build(SampleStats, sortColumn: 6, ascending: true, maxRows: 50));

        var findRefIndex = rendered.IndexOf("FindReference", StringComparison.Ordinal);
        var compilationIndex = rendered.IndexOf("Workspace_Project_GetCompilation", StringComparison.Ordinal);
        var diagnosticIndex = rendered.IndexOf("Diagnostics_SemanticDiagnostic", StringComparison.Ordinal);

        Assert.True(diagnosticIndex < compilationIndex, "Diagnostics should appear before Compilation");
        Assert.True(compilationIndex < findRefIndex, "Compilation should appear before FindReference");
    }

    [Fact]
    public void Build_SortByCompletedCountDescending_HighestFirst()
    {
        var rendered = RenderToString(
            WorkspaceTableBuilder.Build(SampleStats, sortColumn: 2, ascending: false, maxRows: 50));

        var diagnosticIndex = rendered.IndexOf("Diagnostics_SemanticDiagnostic", StringComparison.Ordinal);
        var compilationIndex = rendered.IndexOf("Workspace_Project_GetCompilation", StringComparison.Ordinal);
        var findRefIndex = rendered.IndexOf("FindReference", StringComparison.Ordinal);

        // Diagnostics (20) > Workspace (10) > FindReference (5)
        Assert.True(diagnosticIndex < compilationIndex, "Diagnostics should appear before Compilation");
        Assert.True(compilationIndex < findRefIndex, "Compilation should appear before FindReference");
    }

    [Fact]
    public void Build_SortByCanceledCountDescending_HighestFirst()
    {
        var rendered = RenderToString(
            WorkspaceTableBuilder.Build(SampleStats, sortColumn: 3, ascending: false, maxRows: 50));

        var diagnosticIndex = rendered.IndexOf("Diagnostics_SemanticDiagnostic", StringComparison.Ordinal);
        var compilationIndex = rendered.IndexOf("Workspace_Project_GetCompilation", StringComparison.Ordinal);
        var findRefIndex = rendered.IndexOf("FindReference", StringComparison.Ordinal);

        // Diagnostics (5) > Workspace (2) > FindReference (0)
        Assert.True(diagnosticIndex < compilationIndex, "Diagnostics should appear before Compilation");
        Assert.True(compilationIndex < findRefIndex, "Compilation should appear before FindReference");
    }

    [Fact]
    public void Build_SortByNameDescending_AlphabeticalReverse()
    {
        var rendered = RenderToString(
            WorkspaceTableBuilder.Build(SampleStats, sortColumn: 1, ascending: false, maxRows: 50));

        var compilationIndex = rendered.IndexOf("Workspace_Project_GetCompilation", StringComparison.Ordinal);
        var findRefIndex = rendered.IndexOf("FindReference", StringComparison.Ordinal);
        var diagnosticIndex = rendered.IndexOf("Diagnostics_SemanticDiagnostic", StringComparison.Ordinal);

        // W > F > D
        Assert.True(compilationIndex < findRefIndex, "Workspace should appear before FindReference");
        Assert.True(findRefIndex < diagnosticIndex, "FindReference should appear before Diagnostics");
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
