using System.Collections.Concurrent;
using System.Globalization;
using Olstakh.CodeAnalysisMonitor.Models;

namespace Olstakh.CodeAnalysisMonitor.Services;

/// <inheritdoc />
internal sealed class WorkspaceStatsAggregator : IWorkspaceStatsAggregator
{
    private readonly ConcurrentDictionary<int, OperationData> _operations = [];
    private volatile IReadOnlyDictionary<int, string> _functionNames = new Dictionary<int, string>();

    /// <inheritdoc />
    public void RegisterFunctionDefinitions(string definitions)
    {
        var names = new Dictionary<int, string>();

        foreach (var line in definitions.AsSpan().EnumerateLines())
        {
            if (line.IsEmpty || line.IsWhiteSpace())
            {
                continue;
            }

            // Format: "<id> <name> <goal>" — first token is the integer id, second is the name
            var trimmed = line.Trim();
            var firstSpace = trimmed.IndexOf(' ');
            if (firstSpace <= 0)
            {
                continue;
            }

            var idSpan = trimmed[..firstSpace];
            if (!int.TryParse(idSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            var rest = trimmed[(firstSpace + 1)..].Trim();
            var secondSpace = rest.IndexOf(' ');
            var name = secondSpace > 0 ? rest[..secondSpace] : rest;

            names[id] = name.ToString();
        }

        _functionNames = names;
    }

    /// <inheritdoc />
    public void RecordBlockCompleted(int functionId, int durationMs)
    {
        var data = _operations.GetOrAdd(functionId, static _ => new OperationData());
        data.Durations.Add(durationMs);
        data.CompletedCount.Add(1);
    }

    /// <inheritdoc />
    public void RecordBlockCanceled(int functionId, int durationMs)
    {
        var data = _operations.GetOrAdd(functionId, static _ => new OperationData());
        data.Durations.Add(durationMs);
        data.CanceledCount.Add(1);
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkspaceOperationStats> GetSnapshot()
    {
        var names = _functionNames;

        return _operations.Select(kvp =>
        {
            var functionId = kvp.Key;
            var data = kvp.Value;
            var durations = data.Durations.ToArray();
            var count = durations.Length;

            if (count == 0)
            {
                return new WorkspaceOperationStats
                {
                    OperationName = ResolveName(functionId, names),
                    CompletedCount = 0,
                    CanceledCount = 0,
                    AverageDuration = TimeSpan.Zero,
                    P90Duration = TimeSpan.Zero,
                    TotalDuration = TimeSpan.Zero,
                };
            }

            var totalMs = 0L;
            foreach (var d in durations)
            {
                totalMs += d;
            }

            var avgMs = totalMs / count;

            Array.Sort(durations);
            var p90Index = Math.Max(0, (int)Math.Ceiling(count * 0.9) - 1);
            var p90Ms = durations[p90Index];

            return new WorkspaceOperationStats
            {
                OperationName = ResolveName(functionId, names),
                CompletedCount = data.CompletedCount.Sum(),
                CanceledCount = data.CanceledCount.Sum(),
                AverageDuration = TimeSpan.FromMilliseconds(avgMs),
                P90Duration = TimeSpan.FromMilliseconds(p90Ms),
                TotalDuration = TimeSpan.FromMilliseconds(totalMs),
            };
        }).ToList();
    }

    private static string ResolveName(int functionId, IReadOnlyDictionary<int, string> names)
    {
        return names.TryGetValue(functionId, out var name)
            ? name
            : functionId.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class OperationData
    {
        public ConcurrentBag<int> Durations { get; } = [];
        public ConcurrentBag<int> CompletedCount { get; } = [];
        public ConcurrentBag<int> CanceledCount { get; } = [];
    }
}
