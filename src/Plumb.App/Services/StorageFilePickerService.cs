using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Plumb.App.Services;

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
}
