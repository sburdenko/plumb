# Plumb

Desktop viewer for IFC building models. Drop an `.ifc` file and browse its spatial structure and every property, element by element.

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

A plumb line is the weighted string builders hang to check that a wall is true.

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/plumb-dark.png">
  <img alt="Plumb with the Duplex model open: spatial tree on the left, properties of the selected wall on the right" src="docs/plumb-light.png">
</picture>

## What works today

- Reads IFC2X3 and IFC4 files with [xBIM](https://github.com/xBimTeam/XbimEssentials). Both schemas go through the same code via the IFC4 interfaces.
- Builds the spatial tree: project, site, building, storeys sorted by elevation, then elements grouped by type. Parts of aggregates, such as stair flights, sit under their stair and keep its storey.
- Collects instance and type property sets and quantity sets. Instance values override type values. Properties without their own unit get the project's unit for that measure.
- Filters the tree by name or IFC type.
- Imports off the UI thread, with progress and cancel.

The Duplex sample (2.4 MB, 246 elements, 12,713 property values) imports in about 0.4 s on Apple Silicon.

## Roadmap

- [x] IFC reading, spatial tree, properties
- [ ] `.plumb` package: properties in SQLite, reopen without re-importing
- [ ] Geometry to `.glb` with IfcConvert, node names set to IFC GlobalIds
- [ ] Unity 6 viewer: orbit camera, click an element to see its IFC data
- [ ] Numbers on larger models: import time, GLB size, viewer load time and FPS

## Design notes

- **Errors are values.** `ImportService` returns `ImportResult.Success` or `ImportResult.Failure` with an `ImportError` code and never throws for expected failures. xBIM wraps exceptions thrown from its progress callback, so cancellation is detected from the token rather than the exception type.
- **Window states are types.** Empty, importing and loaded are separate view models, and the main view model swaps between them. There are no `IsLoading` flags.
- **Flat records, tree on demand.** The importer emits two flat lists, elements and properties, shaped like the SQLite tables that come next. The tree is built from parent ids in `Plumb.Core`.
- **Shared core.** `Plumb.Core` targets netstandard2.1 and has no dependencies, so the Unity viewer can reuse the model types.
- **No Windows-only storage.** Models open with xBIM's `MemoryModel`, because the default `IfcStore` provider may pick the Esent database, which only runs on Windows.
- **Locale-proof numbers.** Values are written with the invariant culture and 10 significant digits, so Revit's `17.38299999999997` becomes `17.383` on any system.

## Build and run

Requires the .NET 10 SDK. macOS on Apple Silicon is the main target; Windows should work.

```bash
samples/fetch-samples.sh
dotnet run --project src/Plumb.App -- samples/Duplex.ifc
```

Without a path the app opens on an empty drop zone.

```bash
dotnet test
```

The tests import the Duplex model and check the tree, storeys, units and property integrity. They also cover the error paths, cancellation, search and the window state transitions.

The sample model is not stored in the repository because its source publishes no license. `fetch-samples.sh` downloads it from a pinned commit of [youshengCode/IfcSampleFiles](https://github.com/youshengCode/IfcSampleFiles) and verifies its SHA-256.

## Layout

```
src/Plumb.Core      data model, import contract, tree building and search
src/Plumb.Import    IFC reading with xBIM
src/Plumb.App       Avalonia desktop app
tests/Plumb.Tests   NUnit tests
```

## Stack

.NET 10, Avalonia 12, CommunityToolkit.Mvvm, xBIM Essentials 6, NUnit.

Next on the roadmap: IfcConvert for geometry, SQLite for packages, and a Unity 6 viewer with glTFast.

## License

MIT. xBIM is licensed separately under CDDL 1.0.
