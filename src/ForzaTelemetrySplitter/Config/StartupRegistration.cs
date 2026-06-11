using Microsoft.Win32;

namespace ForzaTelemetrySplitter.Config;

/// <summary>
/// Manages "start with Windows" for the current user via the per-user Run key
/// (HKCU\Software\Microsoft\Windows\CurrentVersion\Run). Per-user means no administrator rights are
/// needed. This is the same key the installer writes, so the in-app toggle and the installer option
/// stay consistent — and it gives the portable build (which has no installer) a way to set it.
///
/// All calls are best-effort: registry access is wrapped so a failure never crashes the app.
/// </summary>
public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ForzaTelemetrySplitter";

    /// <summary>True if a Run entry for this app exists.</summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Add the Run entry pointing at this executable. Returns success.</summary>
    public static bool Enable()
    {
        try
        {
            string? exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return false;
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            key.SetValue(ValueName, $"\"{exe}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Remove the Run entry. Returns success (also true if it wasn't present).</summary>
    public static bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
