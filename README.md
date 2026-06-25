# NvForge

**All-in-one NVIDIA GPU manager for Windows** — for both desktop GPUs (RTX 4060)
and laptop/mobile GPUs (RTX 5070 Ti Laptop GPU). Built in phases.

> **Status: Phase 1** — GPU detection + driver customization (NVCleanstall
> orchestration and reversible privacy/update tweaks). Live monitoring,
> overclocking, and the full Windows-tweaks suite are on the roadmap below.

## What it does today

- **Detects your NVIDIA GPU(s)** and tells desktop from laptop/mobile, with VRAM,
  driver version, and the tuning capabilities that part actually supports.
- **Driver Customization**
  - Finds your copy of **NVCleanstall** (or links you to the official download)
    and launches it.
  - Shows **curated "strip vs keep" recommendations** tailored to desktop or
    laptop GPUs.
  - Opens NVIDIA's official driver download page.
  - Applies **reversible** post-install tweaks — disable NVIDIA telemetry and
    driver auto-update checks — each backed up first and undoable with one click.
- **Diagnostics** export for easy troubleshooting.

## Getting the app

NvForge is built automatically by GitHub Actions on a Windows runner.

1. Open the **Actions** tab → latest **build** run.
2. Download the **`NvForge-win-x64`** artifact.
3. Unzip and run **`NvForge.exe`** — it will prompt for Administrator (needed for
   driver tweaks).

No install required; it's a single self-contained `.exe` (the .NET runtime is
bundled).

### Try it without a GPU

Run `NvForge.exe --simulate` to load a mock RTX 4060 + RTX 5070 Ti laptop fleet.
In simulate mode all destructive actions are disabled — useful for a quick look.

## Safety

- Driver tweaks edit the **registry**, **service start types**, and **scheduled
  tasks**. Every change is **backed up to JSON first** (in
  `%LOCALAPPDATA%\NvForge\backups`) and can be reverted with **Restore**.
- **Laptop GPUs** (like the RTX 5070 Ti mobile) usually have power/voltage limits
  locked by the vendor — those controls will be limited or unavailable.
- NVCleanstall is third-party freeware: NvForge **detects and launches your own
  copy**, it does not bundle or modify it.

## Roadmap

| Phase | Feature |
|---|---|
| 1 ✅ | Detection + driver customization (this release) |
| 2 | Live monitoring (clocks, temps, fans, power) via NVML + charts |
| 3 | Overclocking / fan curves via the official NVAPI SDK + per-GPU profiles |
| 4 | Full Windows tweaks (HAGS, Ultimate Performance, MSI mode, NVCP settings) |

## Building from source

Requires the **.NET 8 SDK**. On Windows:

```powershell
dotnet build NvForge.sln -c Release
dotnet test  tests/NvForge.Core.Tests/NvForge.Core.Tests.csproj -c Release
dotnet publish src/NvForge.App/NvForge.App.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o publish
```

The cross-platform `NvForge.Core` library and its tests also build on Linux/macOS;
the Windows projects compile anywhere via `EnableWindowsTargeting` but only run on
Windows. See [`docs/architecture.md`](docs/architecture.md) for the full design.
