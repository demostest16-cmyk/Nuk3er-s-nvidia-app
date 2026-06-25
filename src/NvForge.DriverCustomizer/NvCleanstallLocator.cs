using System.Diagnostics;
using System.Runtime.Versioning;

namespace NvForge.DriverCustomizer;

/// <summary>Where (if anywhere) a copy of NVCleanstall was found.</summary>
public sealed class NvCleanstallLocation
{
    public required string ExecutablePath { get; init; }
    public string? Version { get; init; }
}

/// <summary>
/// Finds an existing NVCleanstall executable and launches it. NvForge never
/// bundles or redistributes NVCleanstall (it is third-party freeware) — it only
/// detects a copy the user already has, or sends them to the official download
/// page.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class NvCleanstallLocator
{
    /// <summary>Official TechPowerUp download page for NVCleanstall.</summary>
    public const string DownloadPageUrl =
        "https://www.techpowerup.com/download/techpowerup-nvcleanstall/";

    /// <summary>
    /// Searches common locations for an NVCleanstall executable. NVCleanstall is
    /// usually a portable single .exe, so the user's Downloads folder is checked
    /// first, then a couple of conventional install paths.
    /// </summary>
    public NvCleanstallLocation? Locate()
    {
        foreach (var candidate in CandidatePaths())
        {
            try
            {
                if (File.Exists(candidate))
                    return new NvCleanstallLocation
                    {
                        ExecutablePath = candidate,
                        Version = FileVersionInfo.GetVersionInfo(candidate).FileVersion,
                    };
            }
            catch
            {
                // ignore unreadable candidates
            }
        }

        // Portable build dropped into Downloads with a versioned name.
        foreach (var dir in PatternSearchDirs())
        {
            try
            {
                if (!Directory.Exists(dir))
                    continue;
                var match = Directory
                    .EnumerateFiles(dir, "NVCleanstall*.exe", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => f)
                    .FirstOrDefault();
                if (match is not null)
                    return new NvCleanstallLocation
                    {
                        ExecutablePath = match,
                        Version = SafeFileVersion(match),
                    };
            }
            catch
            {
                // ignore
            }
        }

        return null;
    }

    /// <summary>Launches NVCleanstall (inherits the current elevated token).</summary>
    public void Launch(string executablePath)
    {
        Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = true });
    }

    /// <summary>Opens the official NVCleanstall download page in the default browser.</summary>
    public void OpenDownloadPage()
    {
        Process.Start(new ProcessStartInfo(DownloadPageUrl) { UseShellExecute = true });
    }

    private static IEnumerable<string> CandidatePaths()
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        yield return Path.Combine(pf, "NVCleanstall", "NVCleanstall.exe");
        yield return Path.Combine(pfx86, "NVCleanstall", "NVCleanstall.exe");
    }

    private static IEnumerable<string> PatternSearchDirs()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(profile, "Downloads");
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private static string? SafeFileVersion(string path)
    {
        try { return FileVersionInfo.GetVersionInfo(path).FileVersion; }
        catch { return null; }
    }
}
