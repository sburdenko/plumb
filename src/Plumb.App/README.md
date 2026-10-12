# Plumb.App

The desktop window, built with Avalonia: start screen with recent models, importing screen with pipeline steps, loaded screen with tree, properties and viewport status, failed screen.

**May:** reference Import and Core, talk to the operating system through the interfaces in `Platform/`, swap between screen view models.

**May not:** reference Ifc, Geometry or Package directly, parse files, or carry pipeline logic. Every screen is its own view model; the main view model swaps between them instead of toggling flags.
