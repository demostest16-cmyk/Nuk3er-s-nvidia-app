using NvForge.Core.Tuning;
using Xunit;

namespace NvForge.Core.Tests;

public class MockGpuTunerTests
{
    [Fact]
    public void DesktopPart_AcceptsOffsets_AndReadsThemBack()
    {
        using var tuner = new MockGpuTuner();
        var apply = tuner.ApplyOffsets(0, 150, 800);
        Assert.True(apply.Success);

        var state = tuner.ReadState(0);
        Assert.True(state.CoreEditable);
        Assert.Equal(150, state.CoreOffsetMhz);
        Assert.Equal(800, state.MemoryOffsetMhz);
    }

    [Fact]
    public void LaptopPart_IsLocked()
    {
        using var tuner = new MockGpuTuner();
        var state = tuner.ReadState(1);
        Assert.False(state.CoreEditable);
        Assert.False(state.MemoryEditable);

        var apply = tuner.ApplyOffsets(1, 100, 100);
        Assert.False(apply.Success);
    }
}

public class OverclockProfileStoreTests : IDisposable
{
    private readonly string _path;

    public OverclockProfileStoreTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "nvforge-tests", Guid.NewGuid().ToString("N"), "profiles.json");
    }

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_path);
        if (dir is not null && Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void Save_Upserts_ByNameCaseInsensitive()
    {
        var store = new OverclockProfileStore(_path);
        store.Save(new OverclockProfile { Name = "Gaming", CoreOffsetMhz = 100, MemoryOffsetMhz = 500 });
        store.Save(new OverclockProfile { Name = "gaming", CoreOffsetMhz = 200, MemoryOffsetMhz = 900 });

        Assert.Single(store.GetAll());
        var p = store.TryGet("GAMING");
        Assert.NotNull(p);
        Assert.Equal(200, p!.CoreOffsetMhz);
        Assert.Equal(900, p.MemoryOffsetMhz);
    }

    [Fact]
    public void Delete_RemovesProfile()
    {
        var store = new OverclockProfileStore(_path);
        store.Save(new OverclockProfile { Name = "Quiet", CoreOffsetMhz = -100, MemoryOffsetMhz = 0 });
        Assert.True(store.Delete("Quiet"));
        Assert.Empty(store.GetAll());
        Assert.False(store.Delete("Quiet"));
    }

    [Fact]
    public void Save_RequiresName()
    {
        var store = new OverclockProfileStore(_path);
        Assert.Throws<ArgumentException>(() => store.Save(new OverclockProfile { Name = " " }));
    }
}
