# How Plumb is put together

This page is for anyone, not only programmers. It explains what the program is made of and what each part is allowed to do. The technical version, with the full diagram, is in the [README](../README.md#architecture).

## The picture

```
  building.ifc  ──▶  Import  ──▶  building.plumb  ──▶  Window  ──▶  3D viewer
  (file from a         (reads,       (a folder Plumb      (tree and     (separate
   CAD program)         converts,     keeps next to        properties)   program)
                        saves)        the source)
```

A user drops an IFC file on the window. Plumb reads it, builds its 3D shape, and saves everything into a `.plumb` folder next to the file. The window shows the building as a tree with the properties of every element. "Open in 3D" starts a second program, the viewer, which reads the same folder.

## The six parts

Each part is a separate project in `src/`. A part may use only the parts listed in its "Uses" column. A test checks this on every build, so the table cannot quietly go stale.

| Part | What it does | Uses | Outside library |
|---|---|---|---|
| **Core** | Defines the words everyone else speaks: what an element is, what a property is, what the result of an import looks like. Has no logic that touches files or screens. | nothing | none |
| **Ifc** | Reads an IFC file and turns it into Core's elements and properties. The only part that understands the IFC format. | Core | xBIM |
| **Geometry** | Turns the IFC file into a 3D shape by running an outside tool, IfcConvert. Stops it on time-out or cancel. | Core | IfcConvert |
| **Package** | Writes elements and properties into the `.plumb` folder and reads them back. The only part that knows how that folder is laid out. | Core | SQLite |
| **Import** | Runs the steps in the right order: check the file, copy it, read it and build geometry at the same time, save, publish. Turns every failure into a result the window can show. | Core, Ifc, Geometry, Package | none |
| **App** | The window: drag and drop, tree, property panel, recent models, menus. Talks only to Import. | Core, Import | Avalonia |

The viewer in `viewer/` is a separate program. It shares nothing with the parts above except the files in the `.plumb` folder.

## Rules the code follows

- **One outside library per part.** xBIM lives only in Ifc, IfcConvert only in Geometry, SQLite only in Package, Avalonia only in App. Replacing one touches one part.
- **Outcomes are named, not flagged.** Instead of `isSaved = false`, the code has a value that is either `Saved` or `NotSaved`, and `NotSaved` carries the reason. The window reads the reason from the value. A test checks that Core has no yes/no properties.
- **Nothing in Core changes after it is made.** Every Core value is read-only. A test checks that too.
- **Expected failures are results, not crashes.** A missing file, a wrong format, a cancelled import: each comes back as a named result with a message for the user.
- **What a user can do is written as tests.** [docs/scenarios.md](scenarios.md) lists every automated test as a sentence. It is generated from the test names by `tools/scenarios.sh`.

## Words you will meet

| Word | Meaning |
|---|---|
| **IFC** | Industry Foundation Classes. The open file format that building design programs use to exchange models. A text file with the extension `.ifc`. |
| **Element** | One thing in the building: a wall, a door, a storey, the building itself. |
| **Property** | A named value attached to an element, such as its fire rating or its height. |
| **GlobalId** | A 22-character code that is unique for every element in an IFC file. Plumb uses it to connect the tree, the database and the 3D shape. |
| **Storey** | A floor of the building. Elements are grouped by storey in the tree. |
| **Spatial structure** | Project, site, building, storeys: the containers that hold elements. |
| **Package** | The `.plumb` folder Plumb saves next to the source file: a database, a list of elements, the 3D shape and a manifest. |
| **Manifest** | A small file inside the package that says which IFC file it came from, when, and whether the 3D shape was built. |
| **Geometry** | The 3D shape of the model, kept as `model.glb`. |
| **glTF, GLB** | A standard 3D file format. GLB is its single-file form. |
| **IfcConvert** | A separate program from IfcOpenShell that turns IFC into glTF. Plumb starts it and waits. |
| **xBIM** | The library Plumb uses to read IFC files. |
| **SQLite** | A small database kept in one file; the package stores elements and properties in it. |
| **Avalonia** | The toolkit that draws Plumb's window on macOS, Windows and Linux. |
| **Viewer** | The second program, made with Unity, that shows the 3D shape and lets you click elements. |
| **Draft** | The hidden folder where an import writes its files before renaming it to the final package, so a failed import never leaves a half-finished package. |
