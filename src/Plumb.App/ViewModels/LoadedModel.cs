using Plumb.Core.Import;

namespace Plumb.App.ViewModels;

/// <summary>A successfully loaded model and how it got here.</summary>
/// <param name="OpenedPath">The .ifc file or .plumb folder the user opened.</param>
/// <param name="OpenedPackage">True when an existing package was opened rather than an IFC file imported.</param>
/// <param name="LoadTime">How long opening took, as the user waited for it.</param>
public sealed record LoadedModel(ImportResult.Success Result, string OpenedPath, bool OpenedPackage, TimeSpan LoadTime);
