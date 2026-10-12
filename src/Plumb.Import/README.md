# Plumb.Import

The pipeline. Orders the steps (validate, snapshot, read and convert side by side, write, publish), reports progress, and turns every expected failure into an `ImportResult`.

**May:** use Ifc, Geometry and Package; own the draft folder and the publish rename; build the production pipeline in `ImportService.CreateDefault`.

**May not:** reference Avalonia or anything from the window, or throw for an expected failure. This is the only project the app talks to for loading models.
