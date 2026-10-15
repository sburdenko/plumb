using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Plumb.App.Platform;

/// <summary>The few Objective-C runtime calls Plumb needs to talk to AppKit directly.</summary>
[SupportedOSPlatform("macos")]
internal static class MacRuntime
{
    private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";

    public static IntPtr Class(string name) => objc_getClass(name);

    public static IntPtr Selector(string name) => sel_registerName(name);

    [DllImport(ObjCRuntime)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCRuntime)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector, int value);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    public static extern IntPtr Send(IntPtr receiver, IntPtr selector, nuint value);
}
