using NvForge.Core.Abstractions;

namespace NvForge.Core.DriverSettings;

/// <summary>
/// In-memory driver-settings service for <c>--simulate</c> and tests. Stores
/// written values so the UI's applied/restored state behaves realistically
/// without touching the real driver.
/// </summary>
public sealed class MockDriverSettingsService : IDriverSettingsService
{
    private readonly Dictionary<uint, uint> _values = new();

    public string SourceName => "Simulated driver settings (mock)";
    public bool Available => true;
    public string? UnavailableReason => null;

    public uint? Read(uint settingId) => _values.TryGetValue(settingId, out var v) ? v : null;

    public DriverSettingsResult Write(uint settingId, uint value)
    {
        _values[settingId] = value;
        return new DriverSettingsResult { Success = true, Message = "Applied (simulated)." };
    }

    public void Dispose() { }
}
