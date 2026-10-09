# Plumb

Desktop viewer for IFC building models. Open an `.ifc` file and browse its spatial structure and the properties of every element.

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
- Imports in the background with progress reporting and cancellation.

A 2.4 MB Revit export with 246 elements and 12,713 property values opens in about 0.4 s on Apple Silicon.

## Architecture

```
src/Plumb.Core      data model, import contract, tree building and search
src/Plumb.Import    IFC reading with xBIM
src/Plumb.App       Avalonia desktop app
tests/Plumb.Tests   NUnit tests
```

- **Errors are values.** `ImportService` returns `ImportResult.Success` or `ImportResult.Failure` with an `ImportError` code and never throws for expected failures. xBIM wraps exceptions thrown from its progress callback, so cancellation is detected from the token rather than the exception type.
- **Window states are types.** Empty, importing and loaded are separate view models, and the main view model swaps between them instead of toggling flags.
- **Flat records, tree on demand.** The importer produces two flat lists, elements and properties, keyed by IFC GlobalId. The tree is built from parent ids in `Plumb.Core`, and search returns a new pruned tree without touching the original.
- **Portable core.** `Plumb.Core` targets netstandard2.1 and has no dependencies, so it can be referenced from any .NET runtime, including Unity.
- **No Windows-only storage.** Models open with xBIM's `MemoryModel`, because the default `IfcStore` provider may pick the Esent database, which only runs on Windows.
- **Locale-independent numbers.** Values are written with the invariant culture and 10 significant digits, so Revit's `17.38299999999997` becomes `17.383` on any system.

## Getting started

Requires the .NET 10 SDK. Developed and tested on macOS (Apple Silicon); the code is cross-platform .NET and Avalonia.

```bash
dotnet run --project src/Plumb.App
```

To open a file on start, pass its path:

```bash
dotnet run --project src/Plumb.App -- path/to/model.ifc
```

## Tests

```bash
samples/fetch-samples.sh
dotnet test
```

The suite imports the Duplex architecture model and checks the tree, storey order, units and property integrity, and covers error handling, cancellation, search and the window state transitions.

The Duplex model is not stored in the repository because its source publishes no license. `fetch-samples.sh` downloads it from a pinned commit of [youshengCode/IfcSampleFiles](https://github.com/youshengCode/IfcSampleFiles) and verifies its SHA-256.

## Built with

.NET 10, Avalonia 12, CommunityToolkit.Mvvm, [xBIM Essentials](https://github.com/xBimTeam/XbimEssentials) 6, NUnit.

## License

MIT. xBIM is licensed separately under CDDL 1.0.
