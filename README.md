# NvForge

**All-in-one NVIDIA GPU manager for Windows** — for both desktop GPUs (RTX 4060)
and laptop/mobile GPUs (RTX 5070 Ti Laptop GPU). Built in phases.

> **Status: Phase 5** — GPU detection, driver customization, live monitoring,
> overclocking/tuning (experimental), a reversible Windows performance tweaks
> suite, and **one-click NVIDIA Control Panel settings**. All four roadmap
> pillars plus driver-level tuning are in the app.

## What it does today

- **Detects your NVIDIA GPU(s)** and tells desktop from laptop/mobile, with VRAM,
  driver version, and the tuning capabilities that part actually supports.
- **Live monitoring** (via NVML) — clocks, temperature, fan, power, GPU/memory
  utilization, and VRAM, updating every second with min/avg/max, a usage history
  graph, and optional CSV logging.
- **Overclocking / tuning** (via NVAPI, *experimental*) — core/memory clock
  offset sliders, capability-gated per GPU, with a 15-second test-then-auto-revert
  safety and savable profiles.
- **Windows performance tweaks** (all reversible, backed up first) — Hardware-
  Accelerated GPU Scheduling, Ultimate Performance power plan, increased TDR
  delay, disable Fullscreen Optimizations, disable Game DVR/Game Bar, and MSI
  interrupt mode for your GPU.
- **NVIDIA Control Panel settings** (via NVAPI, *experimental*) — one-click
  Optimize/Restore for Power Management Mode (Prefer Max Performance), Low
  Latency, Vertical Sync, and Threaded Optimization.
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
| 1 ✅ | Detection + driver customization |
| 2 ✅ | Live monitoring (clocks, temps, fans, power) via NVML |
| 3 ✅ | Overclocking (core/memory offsets) via NVAPI + profiles — *experimental* |
| 4 ✅ | Windows tweaks: HAGS, Ultimate Performance, TDR, FSO, Game DVR, MSI mode |
| 5 ✅ | NVIDIA Control Panel settings (power mode, low latency, V-Sync, threaded opt.) via NVAPI DRS — *experimental* (this release) |

Possible follow-ups: a fan-curve editor, power-limit control (likely via NVML's
simpler API), and on-hardware validation of the NVAPI write paths.

> **Overclocking is experimental.** The NVAPI clock-offset write path is built
> from the public NVAPI definitions but has not yet been validated on real
> hardware. NVAPI rejects malformed requests cleanly (it validates struct
> versions), and Apply auto-reverts after 15 seconds unless you confirm — but
> please treat it as a beta feature and report what you see (use the Diagnostics
> tab). Laptop GPUs are typically locked and will show as non-editable.

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
