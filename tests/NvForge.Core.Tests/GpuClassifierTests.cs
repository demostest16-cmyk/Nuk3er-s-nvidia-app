using NvForge.Core.Gpu;
using NvForge.Core.Models;
using Xunit;

namespace NvForge.Core.Tests;

public class GpuClassifierTests
{
    [Theory]
    [InlineData("NVIDIA GeForce RTX 4060")]
    [InlineData("NVIDIA GeForce GTX 1080 Ti")]
    [InlineData("NVIDIA RTX A6000")]
    public void Classify_DesktopNvidiaParts_AreDesktop(string name)
    {
        var c = GpuClassifier.Classify(name);

        Assert.True(c.IsNvidia);
        Assert.Equal(GpuFormFactor.Desktop, c.FormFactor);
    }

    [Theory]
    [InlineData("NVIDIA GeForce RTX 5070 Ti Laptop GPU")]
    [InlineData("NVIDIA GeForce RTX 4060 Laptop GPU")]
    [InlineData("NVIDIA GeForce RTX 3080 Max-Q")]
    public void Classify_MobileParts_AreMobile(string name)
    {
        var c = GpuClassifier.Classify(name);

        Assert.True(c.IsNvidia);
        Assert.Equal(GpuFormFactor.Mobile, c.FormFactor);
    }

    [Theory]
    [InlineData("AMD Radeon RX 7900 XTX")]
    [InlineData("Intel Arc A770")]
    [InlineData("Microsoft Basic Display Adapter")]
    [InlineData("")]
    [InlineData(null)]
    public void Classify_NonNvidia_IsNotNvidiaAndHasNoCapabilities(string? name)
    {
        var c = GpuClassifier.Classify(name);

        Assert.False(c.IsNvidia);
        Assert.Equal(GpuFormFactor.Unknown, c.FormFactor);
        Assert.Equal(CapabilityFlags.None, c.Capabilities);
    }

    [Fact]
    public void Desktop_ExposesFullTuning_IncludingPowerAndVoltage()
    {
        var c = GpuClassifier.Classify("NVIDIA GeForce RTX 4060");

        Assert.True((c.Capabilities & CapabilityFlags.PowerLimit) != 0);
        Assert.True((c.Capabilities & CapabilityFlags.VoltageControl) != 0);
        Assert.True((c.Capabilities & CapabilityFlags.CoreClockOffset) != 0);
    }

    [Fact]
    public void Mobile_IsGatedForLockedControls()
    {
        var c = GpuClassifier.Classify("NVIDIA GeForce RTX 5070 Ti Laptop GPU");

        // Locked on typical laptops:
        Assert.True((c.Capabilities & CapabilityFlags.PowerLimit) == 0);
        Assert.True((c.Capabilities & CapabilityFlags.TempLimit) == 0);
        Assert.True((c.Capabilities & CapabilityFlags.VoltageControl) == 0);
        // Still allowed:
        Assert.True((c.Capabilities & CapabilityFlags.Monitoring) != 0);
        Assert.True((c.Capabilities & CapabilityFlags.CoreClockOffset) != 0);
        Assert.True((c.Capabilities & CapabilityFlags.DriverCustomization) != 0);
    }
}
