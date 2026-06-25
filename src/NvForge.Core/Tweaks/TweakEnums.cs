namespace NvForge.Core.Tweaks;

/// <summary>The kind of system change a single tweak operation performs.</summary>
public enum TweakOperationKind
{
    RegistryValue = 0,
    ScheduledTask = 1,
    Service = 2,
}

/// <summary>
/// Registry root. Mirrors <c>Microsoft.Win32.RegistryHive</c> but is declared
/// in Core so the cross-platform library carries no Windows dependency.
/// </summary>
public enum RegistryHive
{
    LocalMachine = 0,
    CurrentUser = 1,
    ClassesRoot = 2,
    Users = 3,
}

/// <summary>Subset of registry value types NvForge writes. Mirrors <c>Microsoft.Win32.RegistryValueKind</c>.</summary>
public enum RegistryValueType
{
    String = 0,
    ExpandString = 1,
    Binary = 2,
    Dword = 3,
    MultiString = 4,
    Qword = 5,
}
