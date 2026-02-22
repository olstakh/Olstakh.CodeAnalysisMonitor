# Code Analysis Monitor

A .NET tool that monitors Roslyn source generator performance in real-time via ETW events. Shows a live `top`-like table with invocation counts, average/P90/total durations.

## Install

```
dotnet tool install --global Olstakh.CodeAnalysisMonitor
```

## Usage

Requires an **elevated (Administrator)** terminal — ETW real-time sessions need admin privileges.

```
ca-monitor
```

This starts a live table showing source generator performance as Visual Studio (or any Roslyn compiler) runs generators.

### Options

| Option | Default | Description |
|---|---|---|
| `--top N` | 50 | Maximum number of generators to display |

### Keyboard Controls

| Key | Action |
|---|---|
| `1`–`5` | Sort by column (descending). Press again to toggle ascending. |
| `Ctrl+C` | Exit |

### Columns

1. **Generator** — fully qualified generator type name
2. **Invocations** — total invocation count
3. **Avg Duration** — average duration per invocation
4. **P90 Duration** — 90th percentile duration
5. **Total Duration** — cumulative time spent (default sort)

## Notes

- Data updates when Visual Studio / other IDE triggers design-time builds (on code edits, file saves, solution load) — gaps between updates are normal.
- The table refreshes every 100ms.
