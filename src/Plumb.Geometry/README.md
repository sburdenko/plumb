# Plumb.Geometry

Builds `model.glb` by running IfcConvert as a separate process, with a time limit, and stops the whole process tree on cancel.

**May:** start and watch IfcConvert, report `GeometryState.Built` or `NotBuilt` with the reason.

**May not:** read IFC content itself, know the package layout beyond the output path it is given, or leave a partial file behind. It is the only project that knows IfcConvert exists.
