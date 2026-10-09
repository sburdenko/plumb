using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Plumb.App.Platform;

/// <summary>
/// Sets the Dock icon of a Plumb process that runs outside an app bundle, such as one started by
/// <c>dotnet run</c>. macOS reads the icon of a bundle from its Info.plist and shows a generic one otherwise.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacDockIcon
{
    private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";

    public static bool RunsInsideBundle =>
        AppContext.BaseDirectory.Contains(".app/Contents/MacOS", StringComparison.OrdinalIgnoreCase);

    /// <summary>Sets <paramref name="png"/> as the application icon. Must run on the main thread.</summary>
    /// <exception cref="InvalidOperationException">AppKit could not read the image.</exception>
    public static void Apply(byte[] png)
    {
        var buffer = Marshal.AllocHGlobal(png.Length);
        try
        {
            Marshal.Copy(png, 0, buffer, png.Length);
            // dataWithBytes:length: copies the bytes, so the buffer can be freed right after.
            var data = Send(Class("NSData"), Selector("dataWithBytes:length:"), buffer, (nuint)png.Length);
            var image = Send(Send(Class("NSImage"), Selector("alloc")), Selector("initWithData:"), data);
            if (image == IntPtr.Zero)
            {
                throw new InvalidOperationException("AppKit could not read the Dock icon.");
            }

            var application = Send(Class("NSApplication"), Selector("sharedApplication"));
            Send(application, Selector("setApplicationIconImage:"), image);
            Send(image, Selector("release"));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IntPtr Class(string name) => objc_getClass(name);

    private static IntPtr Selector(string name) => sel_registerName(name);

    [DllImport(ObjCRuntime)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCRuntime)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length);
}
