using NvForge.Core.DriverSettings;
using Xunit;

namespace NvForge.Core.Tests;

public class DriverSettingsCatalogTests
{
    [Fact]
    public void All_IncludesPowerLatencyVsyncAndThreadedSettings()
    {
        var ids = DriverSettingsCatalog.All().Select(s => s.Id).ToList();
        Assert.Contains("power-management-mode", ids);
        Assert.Contains("low-latency", ids);
        Assert.Contains("vertical-sync", ids);
        Assert.Contains("threaded-optimization", ids);
    }

    [Fact]
    public void PowerManagementMode_OptimizesToPreferMax_AndDefaultsToAdaptive()
    {
        var s = DriverSettingsCatalog.All().Single(x => x.Id == "power-management-mode");
        Assert.Equal(DriverSettingsCatalog.PreferredPstateId, s.SettingId);
        Assert.Equal(DriverSettingsCatalog.PreferMaxPerformance, s.OptimizedValue);
        Assert.Equal(DriverSettingsCatalog.PstateAdaptive, s.DefaultValue);
    }

    [Fact]
    public void EverySetting_HasDistinctOptimizedAndDefaultValues()
    {
        Assert.All(DriverSettingsCatalog.All(), s => Assert.NotEqual(s.OptimizedValue, s.DefaultValue));
    }
}

public class MockDriverSettingsServiceTests
{
    [Fact]
    public void Read_IsNullUntilWritten_ThenReflectsValue()
    {
        using var service = new MockDriverSettingsService();
        Assert.True(service.Available);
        Assert.Null(service.Read(DriverSettingsCatalog.VsyncModeId));

        var result = service.Write(DriverSettingsCatalog.VsyncModeId, DriverSettingsCatalog.VsyncForceOff);
        Assert.True(result.Success);
        Assert.Equal(DriverSettingsCatalog.VsyncForceOff, service.Read(DriverSettingsCatalog.VsyncModeId));
    }
}
