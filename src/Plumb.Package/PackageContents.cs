using Plumb.Core.Geometry;
using Plumb.Core.Model;
using Plumb.Core.Package;

namespace Plumb.Package;

public sealed record PackageContents(IfcModelData Model, PackageManifest Manifest, GeometryState Geometry);
