using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>
/// Peak resident memory from getrusage: this process, and the largest child process (IfcConvert).
/// .NET's PeakWorkingSet64 reads 0 on macOS, so the OS is asked directly.
/// </summary>
internal static class PeakMemory
{
    private const int Self = 0;
    private const int Children = -1;

    public static string Of(bool children)
    {
        if (OperatingSystem.IsWindows() || getrusage(children ? Children : Self, out var usage) != 0)
        {
            return "n/a";
        }

        // macOS reports ru_maxrss in bytes, Linux in kilobytes.
        var bytes = OperatingSystem.IsMacOS() ? usage.MaxResidentSet : usage.MaxResidentSet * 1024;
        return $"{(bytes / 1e9).ToString("0.0", CultureInfo.InvariantCulture)} GB";
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int getrusage(int who, out ResourceUsage usage);

    // struct rusage: two timevals, then ru_maxrss and 13 more longs; the same offsets on macOS and Linux (64-bit).
    [StructLayout(LayoutKind.Sequential)]
    private struct ResourceUsage
    {
        public long UserSeconds;
        public long UserMicroseconds;
        public long SystemSeconds;
        public long SystemMicroseconds;
        public long MaxResidentSet;
        public long SharedMemory;
        public long UnsharedData;
        public long UnsharedStack;
        public long MinorFaults;
        public long MajorFaults;
        public long Swaps;
        public long BlockInputs;
        public long BlockOutputs;
        public long MessagesSent;
        public long MessagesReceived;
        public long Signals;
        public long VoluntarySwitches;
        public long InvoluntarySwitches;
    }
}
