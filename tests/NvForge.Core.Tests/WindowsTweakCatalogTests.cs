using NvForge.Core.Tweaks;
using Xunit;

namespace NvForge.Core.Tests;

public class WindowsTweakCatalogTests
{
    [Fact]
    public void All_IncludesExpectedTweaks()
    {
        var ids = WindowsTweakCatalog.All().Select(t => t.Id).ToList();
        Assert.Contains(WindowsTweakCatalog.HagsId, ids);
        Assert.Contains(WindowsTweakCatalog.UltimatePerformanceId, ids);
        Assert.Contains(WindowsTweakCatalog.TdrDelayId, ids);
        Assert.Contains(WindowsTweakCatalog.GameDvrId, ids);
    }

    [Fact]
    public void Hags_SetsHwSchModeToTwo_AndRequiresReboot()
    {
        var hags = WindowsTweakCatalog.Hags();
        Assert.True(hags.RequiresReboot);
        var op = Assert.Single(hags.Operations);
        Assert.Equal(TweakOperationKind.RegistryValue, op.Kind);
        Assert.Equal("HwSchMode", op.ValueName);
        Assert.Equal("2", op.DesiredData);
    }

    [Fact]
    public void UltimatePerformance_UsesPowerSchemeOperation_WithTemplateGuid()
    {
        var up = WindowsTweakCatalog.UltimatePerformance();
        var op = Assert.Single(up.Operations);
        Assert.Equal(TweakOperationKind.PowerScheme, op.Kind);
        Assert.Equal(WindowsTweakCatalog.UltimatePerformanceTemplateGuid, op.Path);
        Assert.True(op.DesiredEnabled);
    }

    [Fact]
    public void MsiModeForGpu_BuildsDeviceScopedRegistryPath()
    {
        const string pnp = @"PCI\VEN_10DE&DEV_2882&SUBSYS_00000000&REV_A1\4&abcd&0&0019";
        var tweak = WindowsTweakCatalog.MsiModeForGpu(pnp);

        Assert.Contains(pnp, tweak.Id);
        var op = Assert.Single(tweak.Operations);
        Assert.Equal(TweakOperationKind.RegistryValue, op.Kind);
        Assert.Contains(pnp, op.Path);
        Assert.Contains("MessageSignaledInterruptProperties", op.Path);
        Assert.Equal("MSISupported", op.ValueName);
        Assert.Equal("1", op.DesiredData);
    }
}
