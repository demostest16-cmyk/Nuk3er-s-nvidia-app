using System.Diagnostics;
using System.Runtime.Versioning;
using NvForge.Core.Models;

namespace NvForge.DriverCustomizer;

/// <summary>Progress of a driver download.</summary>
public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes)
{
    public double? Percent => TotalBytes is > 0 ? 100.0 * BytesReceived / TotalBytes.Value : null;
}

/// <summary>
/// Helps the user obtain an official NVIDIA driver. The primary path opens
/// NVIDIA's own driver page (its auto-detect reliably matches both desktop and
/// laptop GPUs); a generic streaming downloader is also provided for when a
/// direct driver URL is known. Fully automated model→driver resolution is a
/// later enhancement (it needs NVIDIA's product-id tables).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DriverDownloadService
{
    public const string GeForceDriversPage = "https://www.nvidia.com/en-us/geforce/drivers/";
    public const string ManualSearchPage = "https://www.nvidia.com/Download/index.aspx";

    private readonly HttpClient _http;

    public DriverDownloadService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
    }

    /// <summary>Opens NVIDIA's official driver download page in the browser.</summary>
    public void OpenDownloadPage(GpuInfo? gpu = null)
    {
        // The GeForce drivers page auto-detects the installed GPU; we pass the
        // user there rather than guessing a fragile deep link.
        Process.Start(new ProcessStartInfo(GeForceDriversPage) { UseShellExecute = true });
    }

    public void OpenManualSearchPage()
    {
        Process.Start(new ProcessStartInfo(ManualSearchPage) { UseShellExecute = true });
    }

    /// <summary>
    /// Streams a file (e.g. a driver package) to <paramref name="destinationPath"/>,
    /// reporting progress. Returns the destination path on success.
    /// </summary>
    public async Task<string> DownloadAsync(
        Uri url,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var dest = File.Create(destinationPath);

        var buffer = new byte[81920];
        long received = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            received += read;
            progress?.Report(new DownloadProgress(received, total));
        }

        return destinationPath;
    }
}
