using NvForge.Core.Models;

namespace NvForge.Core.Drivers;

/// <summary>
/// Builds the curated "what to strip" guidance shown before launching
/// NVCleanstall. Recommendations differ slightly by form factor — laptops keep
/// display-switching / Optimus plumbing that a desktop does not need.
/// </summary>
public static class DriverRecommendationBuilder
{
    public static IReadOnlyList<DriverComponentRecommendation> Build(GpuFormFactor formFactor)
    {
        var list = new List<DriverComponentRecommendation>
        {
            new()
            {
                Id = "display-driver",
                Name = "Display Driver",
                Category = "Core",
                Recommended = ComponentAction.Keep,
                Reason = "The actual GPU driver. Always required.",
            },
            new()
            {
                Id = "physx",
                Name = "PhysX System Software",
                Category = "Gaming",
                Recommended = ComponentAction.Keep,
                Reason = "Small; a handful of games still rely on it. Low cost to keep.",
            },
            new()
            {
                Id = "geforce-experience",
                Name = "GeForce Experience / NVIDIA App",
                Category = "Bloat",
                Recommended = ComponentAction.Strip,
                Reason = "Background services, login, overlay. Strip unless you use ShadowPlay/auto-optimize.",
            },
            new()
            {
                Id = "telemetry",
                Name = "Telemetry & Crash Reporter",
                Category = "Privacy",
                Recommended = ComponentAction.Strip,
                Reason = "Usage/telemetry collection. NvForge can also disable it post-install.",
            },
            new()
            {
                Id = "hd-audio",
                Name = "HD Audio Driver",
                Category = "Audio",
                Recommended = ComponentAction.Strip,
                Reason = "Only needed for audio over HDMI/DisplayPort. Strip if you use other audio output.",
            },
            new()
            {
                Id = "nvcontainer",
                Name = "NVIDIA Container (nvContainer)",
                Category = "Services",
                Recommended = ComponentAction.Strip,
                Reason = "Hosts optional background tasks. Strip when GeForce Experience is removed.",
            },
            new()
            {
                Id = "stereo-3d",
                Name = "Stereoscopic 3D / 3D Vision",
                Category = "Legacy",
                Recommended = ComponentAction.Strip,
                Reason = "Discontinued feature. Safe to remove.",
            },
        };

        if (formFactor == GpuFormFactor.Mobile)
        {
            list.Add(new DriverComponentRecommendation
            {
                Id = "usb-c-nvusb",
                Name = "USB-C / NVIDIA USB Controller",
                Category = "Display",
                Recommended = ComponentAction.Keep,
                Reason = "Laptops often route external displays / docks through this. Keep on mobile.",
            });
            list.Add(new DriverComponentRecommendation
            {
                Id = "optimus",
                Name = "Optimus / Display Switching",
                Category = "Display",
                Recommended = ComponentAction.Keep,
                Reason = "Required for hybrid graphics (iGPU + dGPU) on laptops. Do not strip.",
            });
        }
        else
        {
            list.Add(new DriverComponentRecommendation
            {
                Id = "usb-c-nvusb",
                Name = "USB-C / NVIDIA USB Controller",
                Category = "Display",
                Recommended = ComponentAction.Strip,
                Reason = "Only needed for VR headsets / USB-C displays. Strip on a typical desktop.",
            });
        }

        return list;
    }
}
