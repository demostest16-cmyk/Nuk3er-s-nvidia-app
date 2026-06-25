using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace NvForge.DriverCustomizer;

/// <summary>
/// Reads and switches the active Windows power scheme via <c>powercfg.exe</c>.
/// Used to enable the hidden Ultimate Performance plan (reversibly).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed partial class PowerSchemeController
{
    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidRegex();

    public string? GetActiveSchemeGuid()
    {
        var result = ProcessRunner.Run("powercfg", "/getactivescheme");
        return result.Success ? ExtractGuid(result.StandardOutput) : null;
    }

    /// <summary>Duplicates a scheme template and returns the new scheme's GUID (or null).</summary>
    public string? DuplicateScheme(string templateGuid)
    {
        var result = ProcessRunner.Run("powercfg", "-duplicatescheme", templateGuid);
        return result.Success ? ExtractGuid(result.StandardOutput) : null;
    }

    public bool SetActive(string guid)
    {
        return ProcessRunner.Run("powercfg", "/setactive", guid).Success;
    }

    private static string? ExtractGuid(string text)
    {
        var match = GuidRegex().Match(text);
        return match.Success ? match.Value : null;
    }
}
