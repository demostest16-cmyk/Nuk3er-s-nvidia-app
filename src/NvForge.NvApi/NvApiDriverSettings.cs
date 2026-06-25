using System.Runtime.Versioning;
using NvForge.Core.Abstractions;

namespace NvForge.NvApi;

/// <summary>
/// Reads/writes global NVIDIA Control Panel settings via NVAPI's Driver Settings
/// (DRS) API. Each operation opens, loads, mutates, saves, and destroys a DRS
/// session against the base (system-wide) profile.
///
/// Like the overclock tuner, this is built from public NVAPI definitions and
/// validated only on hardware; NVAPI version-checks the struct, so a mismatch
/// fails cleanly (returns an error) rather than corrupting state.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class NvApiDriverSettings : IDriverSettingsService
{
    private readonly NvUnload? _unload;
    private readonly NvDrsCreateSession? _create;
    private readonly NvDrsDestroySession? _destroy;
    private readonly NvDrsLoadSettings? _load;
    private readonly NvDrsSaveSettings? _save;
    private readonly NvDrsGetBaseProfile? _getBaseProfile;
    private readonly NvDrsGetSetting? _getSetting;
    private readonly NvDrsSetSetting? _setSetting;
    private bool _initialized;

    public NvApiDriverSettings()
    {
        try
        {
            var initialize = NvApiInterop.GetDelegate<NvInitialize>(NvApiIds.Initialize);
            if (initialize is null || initialize() != NvApiConst.NvApiOk)
            {
                UnavailableReason = "NVAPI_Initialize failed. Is an NVIDIA driver installed?";
                return;
            }

            _initialized = true;
            _unload = NvApiInterop.GetDelegate<NvUnload>(NvApiIds.Unload);
            _create = NvApiInterop.GetDelegate<NvDrsCreateSession>(NvApiIds.DRS_CreateSession);
            _destroy = NvApiInterop.GetDelegate<NvDrsDestroySession>(NvApiIds.DRS_DestroySession);
            _load = NvApiInterop.GetDelegate<NvDrsLoadSettings>(NvApiIds.DRS_LoadSettings);
            _save = NvApiInterop.GetDelegate<NvDrsSaveSettings>(NvApiIds.DRS_SaveSettings);
            _getBaseProfile = NvApiInterop.GetDelegate<NvDrsGetBaseProfile>(NvApiIds.DRS_GetBaseProfile);
            _getSetting = NvApiInterop.GetDelegate<NvDrsGetSetting>(NvApiIds.DRS_GetSetting);
            _setSetting = NvApiInterop.GetDelegate<NvDrsSetSetting>(NvApiIds.DRS_SetSetting);

            if (!Available)
                UnavailableReason = "NVAPI DRS entry points unavailable.";
        }
        catch (DllNotFoundException)
        {
            UnavailableReason = "nvapi64.dll not found. Install the NVIDIA driver (expected off real hardware).";
        }
        catch (Exception ex)
        {
            UnavailableReason = $"NVAPI error: {ex.Message}";
        }
    }

    public string SourceName => "NVAPI driver settings (DRS)";

    public bool Available => _initialized
        && _create is not null && _destroy is not null && _load is not null
        && _save is not null && _getBaseProfile is not null
        && _getSetting is not null && _setSetting is not null;

    public string? UnavailableReason { get; private set; }

    public uint? Read(uint settingId)
    {
        if (!Available)
            return null;

        IntPtr session = IntPtr.Zero;
        try
        {
            if (_create!(out session) != NvApiConst.NvApiOk)
                return null;
            if (_load!(session) != NvApiConst.NvApiOk)
                return null;
            if (_getBaseProfile!(session, out var profile) != NvApiConst.NvApiOk)
                return null;

            var setting = NvApiInterop.NewSetting();
            var status = _getSetting!(session, profile, settingId, ref setting);
            if (status != NvApiConst.NvApiOk)
                return null; // includes NVAPI_SETTING_NOT_FOUND (unset / default)

            return BitConverter.ToUInt32(setting.currentValue, 0);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (session != IntPtr.Zero)
                _destroy?.Invoke(session);
        }
    }

    public DriverSettingsResult Write(uint settingId, uint value)
    {
        if (!Available)
            return new DriverSettingsResult { Success = false, Message = UnavailableReason ?? "NVAPI DRS unavailable." };

        IntPtr session = IntPtr.Zero;
        try
        {
            if (_create!(out session) != NvApiConst.NvApiOk)
                return Fail("create session");
            if (_load!(session) != NvApiConst.NvApiOk)
                return Fail("load settings");
            if (_getBaseProfile!(session, out var profile) != NvApiConst.NvApiOk)
                return Fail("get base profile");

            var setting = NvApiInterop.NewSetting();
            setting.settingId = settingId;
            setting.settingType = NvApiConst.DwordSettingType;
            setting.settingLocation = NvApiConst.CurrentProfileLocation;
            BitConverter.GetBytes(value).CopyTo(setting.currentValue, 0);

            var setStatus = _setSetting!(session, profile, ref setting);
            if (setStatus != NvApiConst.NvApiOk)
                return new DriverSettingsResult { Success = false, Message = $"NVAPI_DRS_SetSetting returned {setStatus}." };

            var saveStatus = _save!(session);
            return saveStatus == NvApiConst.NvApiOk
                ? new DriverSettingsResult { Success = true, Message = "Applied." }
                : new DriverSettingsResult { Success = false, Message = $"NVAPI_DRS_SaveSettings returned {saveStatus}." };
        }
        catch (Exception ex)
        {
            return new DriverSettingsResult { Success = false, Message = ex.Message };
        }
        finally
        {
            if (session != IntPtr.Zero)
                _destroy?.Invoke(session);
        }
    }

    public void Dispose()
    {
        if (_initialized)
        {
            try { _unload?.Invoke(); } catch { /* ignore */ }
            _initialized = false;
        }
    }

    private static DriverSettingsResult Fail(string step) =>
        new() { Success = false, Message = $"NVAPI DRS {step} failed." };
}
