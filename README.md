# Plumb

[![CI](https://github.com/sburdenko/plumb/actions/workflows/ci.yml/badge.svg)](https://github.com/sburdenko/plumb/actions/workflows/ci.yml)

Desktop viewer for IFC building models. Open an `.ifc` file, browse its spatial structure and the properties of every element, and keep the result as a `.plumb` package that reopens in milliseconds.

```
              ________________________
             /                       /|    IfcProject "0001"
            /                       / |    └── IfcSite "Default"
    _______/_______________________/  |        └── IfcBuilding
    |      |                       | /|            ├── Roof
    |      |  Roof                 |/ |            ├── Level 2
    |      |_______________________|  |            ├── Level 1
    |      |                       | /|            │   ├── IfcDoor (6)
    |      |  Level 2              |/ |            │   ├── IfcStair (2)
    |      |_______________________|  |            │   ├── IfcWallStandardCase (21)
    |      |                       | /|            │   └── + 5 more types
    |      |  Level 1              |/ |            └── T/FDN
    |      |_______________________|  |
    |      |                       | /
   _|_     |  T/FDN                |/
   \ /     |_______________________|
    V
 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
```
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/plumb-dark.png">
  <img alt="Plumb with the Duplex model open: spatial tree on the left, properties of the selected wall on the right" src="docs/plumb-light.png">
</picture>

## Features

- Opens IFC2X3 and IFC4 files by drag and drop or from a file dialog.
- Shows the spatial structure: project, site, building and storeys sorted by elevation, with elements grouped by IFC type. Parts of aggregates, such as stair flights, appear under their parent element and keep its storey.
- Shows instance and type property sets and quantity sets for the selected element. Instance values override type values.
- Resolves units from the project's unit assignment when a property does not specify its own.
- Filters the tree by element name or IFC type.
- Saves every import as a `.plumb` package next to the source file. Opening the package skips IFC parsing entirely. When that folder is read-only, the model still opens and the app shows why it was not saved.
- Imports in the background with progress and cancellation. A cancelled or failed import never leaves a half-written package behind.

| Duplex sample: 2.4 MB, 246 elements, 12,713 property values | Time |
|---|---|
| Import from IFC | ~260 ms |
| Open the saved package | ~15 ms |

Measured on Apple Silicon with a Release build in a warm process.

## Architecture

Every project does one job and references only what that job needs. Each box depends only on the boxes below it.

```
                  ┌────────────────────────────┐
                  │         Plumb.App          │
                  │    Avalonia desktop UI     │
                  └──────────────┬─────────────┘
                                 │
                  ┌──────────────┴─────────────┐
                  │        Plumb.Import        │
                  │     runs the pipeline      │
                  └─────┬────────────────┬─────┘
                        │                │
                        │                │
┌───────────────────────┴────┐      ┌────┴───────────────────────┐
│         Plumb.Ifc          │      │       Plumb.Package        │
│    IFC file to records     │      │ records to .plumb and back │
│          [ xBIM ]          │      │         [ SQLite ]         │
└──────────────────────┬─────┘      └─────┬──────────────────────┘
                       │                  │
                       │                  │
                  ┌────┴──────────────────┴────┐
                  │         Plumb.Core         │
                  │   records, results, tree   │
                  │  netstandard2.1, no deps   │
                  └────────────────────────────┘
```

| Project | Job | Key library |
|---|---|---|
| `Plumb.Core` | Records, import results, the element tree and search. Targets netstandard2.1, so any .NET runtime can reference it, Unity included. | |
| `Plumb.Ifc` | Reads an IFC file into element and property records. | xBIM |
| `Plumb.Package` | Writes records into a `.plumb` folder and reads them back. | SQLite |
| `Plumb.Import` | Runs the steps in order, reports progress, turns every failure into a result. | |
| `Plumb.App` | Drag and drop, tree, property panel. | Avalonia |

xBIM and SQLite each appear in exactly one project, so either can be replaced without touching the rest.

### Import pipeline

```
Duplex.ifc
   |
   |-- 1  validate   extension and ISO-10303-21 header        Plumb.Ifc
   |-- 2  read       spatial structure, properties, units     Plumb.Ifc
   |-- 3  write      dot-prefixed draft next to the target    Plumb.Package
   '-- 4  publish    rename the draft to Duplex.plumb         Plumb.Import
                     (an existing package is replaced only here)
```

Cancelling or failing at any step deletes the draft and leaves an existing package untouched. Because the draft sits in the same folder as the target, publishing is a rename, not a copy.

### Package format

```
Duplex.plumb/
|-- manifest.json   format version, source name and SHA-256, schema, element count, import time
|-- model.sqlite    elements and properties tables, properties indexed by GlobalId
'-- elements.json   id, type, name and storey of every element, for viewers without SQLite
```

The manifest is written last, so a folder without one was never finished. Readers check its format version before touching the database.

### Design decisions

- **Errors are values.** The pipeline returns `ImportResult.Success` or `ImportResult.Failure` with an `ImportError` code and never throws for expected failures. xBIM wraps exceptions thrown from its progress callback, so cancellation is detected from the token rather than the exception type.
- **Window states are types.** Empty, importing and loaded are separate view models, and the main view model swaps between them instead of toggling flags.
- **Flat records, tree on demand.** Import produces two flat lists keyed by IFC GlobalId, which map one to one onto the SQLite tables. The tree is built from parent ids, and search returns a new pruned tree without touching the original.
- **No Windows-only storage.** Models open with xBIM's `MemoryModel`, because the default `IfcStore` provider may pick the Esent database, which only runs on Windows.
- **Locale-independent numbers.** Values are written with the invariant culture and 10 significant digits, so Revit's `17.38299999999997` becomes `17.383` on any system.

## Getting started

Requires the .NET 10 SDK. Developed and tested on macOS (Apple Silicon); the code is cross-platform .NET and Avalonia.

```bash
dotnet run --project src/Plumb.App
```

To open a file or package on start, pass its path:

```bash
dotnet run --project src/Plumb.App -- path/to/model.ifc
```

## Tests

```bash
samples/fetch-samples.sh
dotnet test
```

The suite imports the Duplex architecture model and checks the tree, storey order, units and property integrity. It writes and reopens packages, replaces them, cancels mid-write and verifies that no draft is left behind, and covers error handling, search and the window state transitions.

The Duplex model is not stored in the repository because its source publishes no license. `fetch-samples.sh` downloads it from a pinned commit of [youshengCode/IfcSampleFiles](https://github.com/youshengCode/IfcSampleFiles) and verifies its SHA-256.

## Built with

.NET 10, Avalonia 12, CommunityToolkit.Mvvm, [xBIM Essentials](https://github.com/xBimTeam/XbimEssentials) 6, Microsoft.Data.Sqlite, NUnit.

## License

MIT. xBIM is licensed separately under CDDL 1.0.
