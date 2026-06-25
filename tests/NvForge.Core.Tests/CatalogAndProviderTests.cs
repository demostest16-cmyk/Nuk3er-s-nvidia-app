using NvForge.Core.Drivers;
using NvForge.Core.Gpu;
using NvForge.Core.Models;
using NvForge.Core.Tweaks;
using Xunit;

namespace NvForge.Core.Tests;

public class DriverRecommendationBuilderTests
{
    [Fact]
    public void Build_AlwaysKeepsDisplayDriver_AndStripsTelemetry()
    {
        var recs = DriverRecommendationBuilder.Build(GpuFormFactor.Desktop);

        Assert.Contains(recs, r => r.Id == "display-driver" && r.Recommended == ComponentAction.Keep);
        Assert.Contains(recs, r => r.Id == "telemetry" && r.Recommended == ComponentAction.Strip);
    }

    [Fact]
    public void Desktop_StripsUsbC_ButMobileKeepsItAndOptimus()
    {
        var desktop = DriverRecommendationBuilder.Build(GpuFormFactor.Desktop);
        var mobile = DriverRecommendationBuilder.Build(GpuFormFactor.Mobile);

        Assert.Contains(desktop, r => r.Id == "usb-c-nvusb" && r.Recommended == ComponentAction.Strip);
        Assert.Contains(mobile, r => r.Id == "usb-c-nvusb" && r.Recommended == ComponentAction.Keep);
        Assert.Contains(mobile, r => r.Id == "optimus" && r.Recommended == ComponentAction.Keep);
        Assert.DoesNotContain(desktop, r => r.Id == "optimus");
    }
}

public class TweakCatalogTests
{
    [Fact]
    public void All_ContainsTelemetryAndUpdateTweaks_WithStableIds()
    {
        var all = TweakCatalog.All();

        Assert.Contains(all, t => t.Id == TweakCatalog.DisableTelemetryId);
        Assert.Contains(all, t => t.Id == TweakCatalog.DisableUpdateChecksId);
        Assert.All(all, t => Assert.NotEmpty(t.Operations));
    }

    [Fact]
    public void Telemetry_DisablesServiceAndTasks_AndOptsOutViaRegistry()
    {
        var t = TweakCatalog.DisableTelemetry();

        Assert.Contains(t.Operations, o =>
            o.Kind == TweakOperationKind.Service && o.Path == "NvTelemetryContainer" && !o.DesiredEnabled);
        Assert.Contains(t.Operations, o =>
            o.Kind == TweakOperationKind.ScheduledTask && o.Path == "NvTm" && !o.DesiredEnabled);
        Assert.Contains(t.Operations, o =>
            o.Kind == TweakOperationKind.RegistryValue && o.ValueName == "OptInOrOutPreference" && o.DesiredData == "0");
    }
}

public class MockGpuInfoProviderTests
{
    [Fact]
    public void GetGpus_ReturnsDesktop4060AndMobile5070Ti()
    {
        var gpus = new MockGpuInfoProvider().GetGpus();

        Assert.Equal(2, gpus.Count);

        var desktop = Assert.Single(gpus, g => g.FormFactor == GpuFormFactor.Desktop);
        Assert.Contains("4060", desktop.Name);
        Assert.True(desktop.IsNvidia);
        Assert.True(desktop.Supports(CapabilityFlags.PowerLimit));

        var mobile = Assert.Single(gpus, g => g.FormFactor == GpuFormFactor.Mobile);
        Assert.Contains("5070", mobile.Name);
        Assert.True(mobile.IsVendorLocked);
        Assert.False(mobile.Supports(CapabilityFlags.PowerLimit));
    }
}
