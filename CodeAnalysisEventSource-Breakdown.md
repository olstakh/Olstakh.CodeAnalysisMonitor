# CodeAnalysisEventSource Events Breakdown

Based on [CodeAnalysisEventSource.Common.cs @ af0287e](https://github.com/dotnet/roslyn/blob/af0287e7e9bb9a50fc6754ef266ba8f68118dc97/src/Compilers/Core/Portable/CodeAnalysisEventSource.Common.cs)

---

## Keywords

| Keyword | Value | Purpose |
|---|---|---|
| `Performance` | `0b001` | Timing / perf events |
| `Correctness` | `0b010` | Correctness diagnostics |
| `AnalyzerLoading` | `0b100` | Analyzer assembly load/unload lifecycle |

## Tasks

| Task | Value |
|---|---|
| `GeneratorDriverRunTime` | 1 |
| `SingleGeneratorRunTime` | 2 |
| `BuildStateTable` | 3 |
| `Compilation` | 4 |
| `AnalyzerAssemblyLoader` | 5 |

---

## Events

### Performance — Generator Driver (Task 1: `GeneratorDriverRunTime`)

| ID | Method | Opcode | Payloads |
|---|---|---|---|
| **1** | `StartGeneratorDriverRunTime` | Start | `string id` |
| **2** | `StopGeneratorDriverRunTime` | Stop | `long elapsedTicks`, `string id` |

These are the **aggregate** start/stop for the entire generator driver pass. The `id` correlates start/stop pairs.

### Performance — Single Generator (Task 2: `SingleGeneratorRunTime`)

| ID | Method | Opcode | Payloads |
|---|---|---|---|
| **3** | `StartSingleGeneratorRunTime` | Start | `string generatorName`, `string assemblyPath`, `string id` |
| **4** | `StopSingleGeneratorRunTime` | Stop | `string generatorName`, `string assemblyPath`, `long elapsedTicks`, `string id` |

Per-generator timing. This is **the only event the tool currently captures** (event 4 / Stop).

### Error — Generator Exception (no task)

| ID | Method | Level | Payloads |
|---|---|---|---|
| **5** | `GeneratorException` | Error | `string generatorName`, `string exception` |

Fires when a source generator throws.

### Correctness — Build State Table (Task 3: `BuildStateTable`)

| ID | Method | Level | Payloads |
|---|---|---|---|
| **6** | `NodeTransform` | Verbose | `int nodeHashCode`, `string name`, `string tableType`, `int previousTable`, `string previousTableContent`, `int newTable`, `string newTableContent`, `int input1`, `int input2` |

Very detailed incremental-pipeline node diagnostics (9 payload fields). High-volume, verbose.

### Performance — Server Compilation (Task 4: `Compilation`)

| ID | Method | Opcode | Payloads |
|---|---|---|---|
| **7** | `StartServerCompilation` | Start | `string name` |
| **8** | `StopServerCompilation` | Stop | `string name` |

Start/stop for a VBCSCompiler server compilation. No elapsed-ticks payload — you'd compute duration from timestamp delta.

### AnalyzerLoading — Assembly Load Context lifecycle (Task 5: `AnalyzerAssemblyLoader`)

| ID | Method | Opcode/Level | Payloads |
|---|---|---|---|
| **9** | `CreateAssemblyLoadContext` | Start | `string directory`, `string? alc` |
| **10** | `DisposeAssemblyLoadContext` | Stop | `string directory`, `string? alc` |
| **11** | `DisposeAssemblyLoadContextException` | Stop/Error | `string directory`, `string errorMessage`, `string? alc` |
| **12** | `CreateNonLockingLoader` | Informational | `string directory` |

ALC creation, disposal, disposal failures, and shadow-copy loader creation.

### AnalyzerLoading — Analyzer References (no task)

| ID | Method | Payloads |
|---|---|---|
| **13** | `AnalyzerReferenceRequestAddToProject` | `string path`, `string projectName` |
| **14** | `AnalyzerReferenceAddedToProject` | `string path`, `string projectName` |
| **15** | `AnalyzerReferenceRequestRemoveFromProject` | `string path`, `string projectName` |
| **16** | `AnalyzerReferenceRemovedFromProject` | `string path`, `string projectName` |
| **17** | `AnanlyzerReferenceRedirected` | `string redirectorType`, `string originalPath`, `string newPath`, `string project` |

Two-phase add/remove tracking (request → completed) plus redirect support. Note the typo "Ananlyzer" in event 17 — that's in Roslyn's source.

### AnalyzerLoading — Assembly Resolution (no task)

| ID | Method | Level | Payloads |
|---|---|---|---|
| **18** | `ResolvedAssembly` | Informational | `string directory`, `string assemblyName`, `string resolver`, `string filePath`, `string alc` |
| **19** | `ResolveAssemblyFailed` | Informational | `string directory`, `string assemblyName` |

Assembly resolution successes and failures within an analyzer ALC.

### Uncategorized

| ID | Method | Level | Payloads |
|---|---|---|---|
| **20** | `ProjectCreated` | Informational | `string projectSystemName`, `string? filePath` |

---

## Observations for refactoring

Currently, the tool is tightly coupled to **event 4 only** (`SingleGeneratorRunTime/Stop`). Looking at the full event surface, there are several natural groupings you could generalize around:

1. **Start/Stop pairs with elapsed ticks** (events 1-4): Generator driver and per-generator timing. The aggregator already handles ticks-based summarization — this pattern extends cleanly to events 1/2.

2. **Start/Stop pairs without elapsed ticks** (events 7/8, 9/10): Server compilation and ALC lifecycle. Duration must be computed from `TraceEvent.TimeStamp` deltas, which would require a new correlation mechanism (matching by `name`/`directory`+`alc`).

3. **Error events** (events 5, 11): Generator exceptions and ALC disposal errors. These have no timing — they'd be better captured as a log/list rather than an aggregation.

4. **Lifecycle / trace events** (events 12-20): Informational events about analyzer loading, reference management, assembly resolution, and project creation. Useful for diagnostic logging but don't map to the current "aggregate by key + ticks" model.


