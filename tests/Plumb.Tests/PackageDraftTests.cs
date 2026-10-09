using Microsoft.Extensions.Logging.Abstractions;
using Plumb.Import;

namespace Plumb.Tests;

[TestFixture]
public sealed class PackageDraftTests
{
    private TempDirectory _temp = null!;
    private string _target = null!;

    [SetUp]
    public void CreateOldPackage()
    {
        _temp = new TempDirectory();
        _target = Path.Combine(_temp.Path, "Duplex.plumb");
        Directory.CreateDirectory(_target);
        File.WriteAllText(Path.Combine(_target, "old.txt"), "previous import");
    }

    [TearDown]
    public void DeleteTemp() => _temp.Dispose();

    [Test]
    public void PublishReplacesTheOldPackageAndCleansUp()
    {
        using (var draft = WriteDraft(Directory.Move))
        {
            draft.Publish();
        }

        Assert.That(File.Exists(Path.Combine(_target, "new.txt")), Is.True);
        Assert.That(File.Exists(Path.Combine(_target, "old.txt")), Is.False);
        Assert.That(HiddenFolders(), Is.Empty);
    }

    [Test]
    public void FailedPublishRestoresTheOldPackage()
    {
        using (var draft = WriteDraft(FailWhenMovingOnto(_target, fromDraft: true)))
        {
            Assert.That(draft.Publish, Throws.InstanceOf<IOException>());
        }

        Assert.That(File.Exists(Path.Combine(_target, "old.txt")), Is.True);
        Assert.That(HiddenFolders(), Is.Empty);
    }

    [Test]
    public void FailedRestoreKeepsTheOldPackageOnDisk()
    {
        using (var draft = WriteDraft(FailWhenMovingOnto(_target, fromDraft: true, fromBackup: true)))
        {
            Assert.That(draft.Publish, Throws.InstanceOf<IOException>().With.Message.EqualTo("draft move failed"));
        }

        var kept = HiddenFolders().Where(d => d.EndsWith(".old", StringComparison.Ordinal)).ToList();
        Assert.That(kept, Has.Count.EqualTo(1), "the only copy of the old package must survive");
        Assert.That(File.Exists(Path.Combine(kept[0], "old.txt")), Is.True);
        Assert.That(HiddenFolders().Any(d => d.EndsWith(".draft", StringComparison.Ordinal)), Is.False);
    }

    private PackageDraft WriteDraft(Action<string, string> move)
    {
        var draft = new PackageDraft(_target, NullLogger.Instance, move);
        Directory.CreateDirectory(draft.Location);
        File.WriteAllText(Path.Combine(draft.Location, "new.txt"), "this import");
        return draft;
    }

    private static Action<string, string> FailWhenMovingOnto(string target, bool fromDraft = false, bool fromBackup = false) =>
        (source, destination) =>
        {
            if (destination == target && source.EndsWith(".draft", StringComparison.Ordinal) && fromDraft)
            {
                throw new IOException("draft move failed");
            }

            if (destination == target && source.EndsWith(".old", StringComparison.Ordinal) && fromBackup)
            {
                throw new IOException("restore failed");
            }

            Directory.Move(source, destination);
        };

    private IEnumerable<string> HiddenFolders() =>
        Directory.GetDirectories(_temp.Path).Where(d => Path.GetFileName(d).StartsWith('.'));
}
