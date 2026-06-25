using NvForge.Core.Tweaks;
using Xunit;

namespace NvForge.Core.Tests;

public class TweakBackupStoreTests : IDisposable
{
    private readonly string _dir;

    public TweakBackupStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "nvforge-tests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private static TweakBackup SampleBackup() => new()
    {
        TweakId = TweakCatalog.DisableTelemetryId,
        TweakName = "Disable NVIDIA telemetry",
        TimestampUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        Operations =
        {
            new TweakOperationBackup
            {
                Kind = TweakOperationKind.RegistryValue,
                Hive = RegistryHive.LocalMachine,
                Path = @"SOFTWARE\NVIDIA Corporation\NvControlPanel2\Client",
                ValueName = "OptInOrOutPreference",
                Existed = true,
                ValueType = RegistryValueType.Dword,
                OriginalData = "1",
            },
            new TweakOperationBackup
            {
                Kind = TweakOperationKind.Service,
                Path = "NvTelemetryContainer",
                Existed = true,
                OriginalData = "true|Automatic",
            },
        },
    };

    [Fact]
    public void Save_ThenLoad_RoundTripsAllFields()
    {
        var store = new TweakBackupStore(_dir);
        var original = SampleBackup();

        store.Save(original);
        var loaded = store.TryLoad(original.TweakId);

        Assert.NotNull(loaded);
        Assert.Equal(original.TweakId, loaded!.TweakId);
        Assert.Equal(original.TweakName, loaded.TweakName);
        Assert.Equal(original.TimestampUtc, loaded.TimestampUtc);
        Assert.Equal(2, loaded.Operations.Count);

        var reg = loaded.Operations[0];
        Assert.Equal(TweakOperationKind.RegistryValue, reg.Kind);
        Assert.Equal(RegistryHive.LocalMachine, reg.Hive);
        Assert.Equal("OptInOrOutPreference", reg.ValueName);
        Assert.Equal(RegistryValueType.Dword, reg.ValueType);
        Assert.Equal("1", reg.OriginalData);
        Assert.True(reg.Existed);

        var svc = loaded.Operations[1];
        Assert.Equal(TweakOperationKind.Service, svc.Kind);
        Assert.Equal("true|Automatic", svc.OriginalData);
    }

    [Fact]
    public void TryLoad_Missing_ReturnsNull()
    {
        var store = new TweakBackupStore(_dir);
        Assert.Null(store.TryLoad("nope"));
        Assert.False(store.Exists("nope"));
    }

    [Fact]
    public void ListAndDelete_BehaveAsExpected()
    {
        var store = new TweakBackupStore(_dir);
        store.Save(SampleBackup());

        Assert.Contains(TweakCatalog.DisableTelemetryId, store.ListTweakIds());
        Assert.True(store.Exists(TweakCatalog.DisableTelemetryId));

        Assert.True(store.Delete(TweakCatalog.DisableTelemetryId));
        Assert.False(store.Exists(TweakCatalog.DisableTelemetryId));
        Assert.Empty(store.ListTweakIds());
        Assert.False(store.Delete(TweakCatalog.DisableTelemetryId));
    }

    [Fact]
    public void Save_RequiresTweakId()
    {
        var store = new TweakBackupStore(_dir);
        Assert.Throws<ArgumentException>(() => store.Save(new TweakBackup { TweakId = "  " }));
    }
}
