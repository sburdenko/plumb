# Plumb.Package

Writes a `.plumb` folder and reads it back: the SQLite database, `elements.json`, `manifest.json`, and the node count of `model.glb`.

**May:** use SQLite and JSON, check the format version, throw `PackageFormatException` or `PackageOutdatedException`.

**May not:** read IFC files, run tools, or decide what happens when a package is outdated; that is Import's call. It is the only project that references SQLite.
