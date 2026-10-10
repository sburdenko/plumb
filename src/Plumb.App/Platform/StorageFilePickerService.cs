using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Plumb.App.Platform;

public sealed class StorageFilePickerService(TopLevel topLevel) : IFilePickerService
{
    private static readonly FilePickerFileType IfcFileType = new("IFC model") { Patterns = ["*.ifc"] };

    public async Task<string?> PickIfcFileAsync()
    {
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open IFC",
            AllowMultiple = false,
            FileTypeFilter = [IfcFileType, FilePickerFileTypes.All],
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> PickPackageAsync()
    {
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Plumb package",
            AllowMultiple = false,
        });

        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }
}
