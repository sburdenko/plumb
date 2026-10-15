using Plumb.App.Platform;

namespace Plumb.Tests;

internal sealed class FakeFilePicker : IFilePickerService
{
    private readonly TaskCompletionSource<string?> _choice = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<string?> PickIfcFileAsync() => _choice.Task;

    public Task<string?> PickPackageAsync() => _choice.Task;

    public void Choose(string? path) => _choice.SetResult(path);

    public void Fail(Exception error) => _choice.SetException(error);
}
