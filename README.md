# Code Analysis Monitor

A .NET tool that monitors Roslyn code analysis events in real-time via ETW. Shows live `top`-like tables for source generator performance and VBCSCompiler server compilation durations.

## Install

```
dotnet tool install --global Olstakh.CodeAnalysisMonitor
```

## Usage

Requires an **elevated (Administrator)** terminal — ETW real-time sessions need admin privileges.

### Generator Command (default)

```
ca-monitor
ca-monitor generator
```

Shows a live table of source generator performance as Visual Studio (or any Roslyn compiler) runs generators. An **Exceptions** column appears automatically when any generator throws an exception.

#### Columns

1. **Generator** — fully qualified generator type name
2. **Invocations** — total invocation count
3. **Avg Duration** — average duration per invocation
4. **P90 Duration** — 90th percentile duration
5. **Total Duration** — cumulative time spent (default sort)
6. **Exceptions** — exception count (shown only when > 0 for any generator, highlighted in red)

### Compilation Command

```
ca-monitor compilation
```

Shows a live table of VBCSCompiler server compilation durations. Events fire during actual builds (not IntelliSense/design-time).

#### Columns

1. **Project** — project name
2. **Compilations** — total compilation count
3. **Avg Duration** — average compilation duration
4. **P90 Duration** — 90th percentile duration
5. **Total Duration** — cumulative time spent (default sort)

### Options

| Option | Default | Description |
|---|---|---|
| `--top N` | 50 | Maximum number of rows to display |

### Keyboard Controls

| Key | Action |
|---|---|
| `1`–`5` (or `1`–`6` if there was exception in a generator) | Sort by column (descending). Press again to toggle ascending. |
| `Ctrl+C` | Exit |

## Notes

- Generator data updates when Visual Studio / other IDE triggers design-time builds (on code edits, file saves, solution load) — gaps between updates are normal.
- Compilation events fire only during actual builds (`dotnet build`, Build in VS), not during IntelliSense.
- The generator table refreshes every 100ms; the compilation table refreshes every 1s.
