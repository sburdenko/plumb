# Plumb.Ifc

Reads an IFC file into Core records: spatial structure, elements, property and quantity sets, units.

**May:** use xBIM, validate that a file is IFC, throw `IfcParseException` for files it cannot read.

**May not:** know about packages, geometry, the import order or the window. It is the only project that references xBIM.
