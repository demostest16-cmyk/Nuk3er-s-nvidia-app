# NvForge — Architecture

NvForge is an all-in-one NVIDIA GPU manager for Windows, built in phases. It
targets both desktop GPUs (e.g. RTX 4060) and laptop/mobile GPUs (e.g. RTX 5070
Ti Laptop GPU), detecting the difference and gating features accordingly.

## Goals

1. **GPU info & live monitoring** — model, VRAM, clocks, temps, fan, power, utilization.
2. **Driver customization** — orchestrate NVCleanstall + apply reversible privacy/update tweaks.
3. **Overclocking / tuning** — clocks, power/thermal limits, fan curves, profiles.
4. **Windows performance tweaks** — HAGS, power plans, MSI mode, control-panel settings.

## Tech stack

| Concern | Choice |
|---|---|
| Language / runtime | C# / .NET 8 (LTS) |
| UI | WPF + MVVM (`CommunityToolkit.Mvvm`) |
| Monitoring (Phase 2) | NVML (`nvml.dll`) via P/Invoke — best Blackwell support |
| Control (Phase 3) | Native C++ shim over the official [NVIDIA/nvapi](https://github.com/NVIDIA/nvapi) SDK (MIT) |
| Detection (Phase 1) | WMI (`Win32_VideoController`) + display-class registry for accurate VRAM |
| Logging | Serilog (rolling file in `%LOCALAPPDATA%\NvForge\logs`) |
| Packaging | `dotnet publish` single-file, self-contained, **untrimmed** (trimming breaks WPF) |
| Elevation | `app.manifest` `requireAdministrator` |

## Project layout

```
NvForge.sln
├─ src/
│  ├─ NvForge.Core/            cross-platform domain: models, CapabilityFlags,
│  │                           GpuClassifier, driver recommendations, tweak
│  │                           definitions + JSON backup store, mock provider
│  ├─ NvForge.Hardware/        (net8.0-windows) WMI GPU detection
│  ├─ NvForge.DriverCustomizer/(net8.0-windows) NVCleanstall locator, driver
│  │                           download, registry/task/service tweak service
│  └─ NvForge.App/             (net8.0-windows, WPF) MVVM UI, --simulate, logging
├─ tests/NvForge.Core.Tests/   xUnit over the pure Core logic (runs on Linux CI)
└─ .github/workflows/build.yml windows-latest: build, test, publish, upload EXE
```

### Why `NvForge.Core` is cross-platform

All hardware-free logic — GPU classification, capability gating, driver
recommendations, tweak definitions, and the JSON backup store — lives in `Core`
(`net8.0`). That keeps it unit-testable on the headless Linux CI runner. The
Windows-only projects (`net8.0-windows`) contain only the actual WMI / registry /
service / scheduled-task I/O.

### Capability gating

`GpuClassifier` maps an adapter name to a `GpuFormFactor` and a conservative set
of `CapabilityFlags`. Desktop parts get full tuning flags; mobile parts omit
`PowerLimit` / `TempLimit` / `VoltageControl` because laptop vBIOSes usually lock
them. The UI reads these flags to hide/disable controls a part cannot honor.
Phase 2/3 will refine the flags against what the driver actually reports.

### Reversible tweaks

`TweakDefinition` (Core) describes a change as a list of `TweakOperation`s
(registry value / scheduled task / service). `RegistryTweakService`
(DriverCustomizer) captures a `TweakBackup` to JSON **before** applying anything,
so every tweak is exactly reversible via Restore.

## Building & verification (no GPU in the dev/CI box)

The development and CI environment has **no NVIDIA GPU and (in dev) no Windows**.
Strategy:

- **Compilation** happens on GitHub Actions `windows-latest` (and, for the
  managed projects, even on Linux via `EnableWindowsTargeting`). CI builds the
  solution, runs the Core tests, and publishes `NvForge.exe` as an artifact.
- **Real-hardware verification** is done by the user on their RTX 4060 desktop
  and RTX 5070 Ti laptop: download the CI artifact, run as admin, and check
  detection, NVCleanstall launch, tweak apply/restore, and diagnostics export.
- **`--simulate`** loads a mock fleet (4060 desktop + 5070 Ti laptop) so the UI
  and capability gating run on any machine, including CI, without a GPU. In
  simulate mode all destructive actions are no-ops.

## Roadmap

- **Phase 1 ✅** detection + driver customization (NVCleanstall orchestration,
  reversible telemetry/update tweaks), app shell, CI.
- **Phase 2 ✅** live monitoring via NVML (`NvForge.Nvml`): sensor tiles, usage
  history sparkline, min/avg/max, CSV logging.
- **Phase 3 ✅ (experimental)** overclocking via NVAPI (`NvForge.NvApi`). Note: a
  **pure-C# `nvapi_QueryInterface` interop** is used instead of a C++ shim — same
  capability, far simpler build, and equally validatable only on hardware. Core/
  memory clock offsets, per-GPU profiles, capability gating, test-then-revert.
- **Phase 4 ✅** Windows tweaks suite (HAGS, Ultimate Performance power plan, TDR
  delay, fullscreen-optimization, Game DVR, MSI mode), all reversible via the
  registry/task/service/power-scheme tweak engine.

### Possible follow-ups

NVIDIA Control Panel settings (low-latency / power mode) via NVAPI DRS, a
fan-curve editor and power-limit control, an elevated broker process, and
on-hardware validation of the NVAPI overclock write path.

### Future hardening

For now the single WPF app runs elevated. A later option is to split privileged
operations into a separate elevated broker process (named-pipe IPC) so the UI can
run unelevated. Not required for Phase 1.
