using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;

namespace WindowsGpsLocation {

    public class SiteUpdateResult {
        public LocationFix Fix { get; init; }
        public double? Elevation { get; init; }
        public string ElevationSource { get; init; }

        public string Summary => string.Format(CultureInfo.InvariantCulture,
            "{0:0.00000}°, {1:0.00000}°, {2} ({3}, ±{4:0} m; elevation: {5})",
            Fix.Latitude, Fix.Longitude,
            Elevation.HasValue ? $"{Elevation.Value:0} m" : "elevation unchanged",
            Fix.IsSatellite ? (Fix.Is3D ? "GPS 3D fix" : "GPS 2D fix") : $"{Fix.Source} position",
            Fix.HorizontalAccuracy, ElevationSource);
    }

    /// <summary>Gets a fix from Windows and writes it to the active profile's site location.</summary>
    public static class SiteUpdater {

        public static async Task<SiteUpdateResult> UpdateSiteAsync(IProfileService profileService, PluginSettings settings,
                                                                    IProgress<string> progress, CancellationToken token) {
            var fix = await LocationService.GetFixAsync(TimeSpan.FromSeconds(settings.FixTimeoutSeconds), progress, token);

            if (settings.RequireSatelliteFix && !fix.IsSatellite) {
                throw new InvalidOperationException($"No GPS satellite fix within {settings.FixTimeoutSeconds} s (only a {fix.Source} position was available). Move the PC outdoors with a clear sky view, or turn off 'Require GPS satellite fix'.");
            }
            if (fix.HorizontalAccuracy > settings.MaxAccuracyMeters) {
                throw new InvalidOperationException($"Position accuracy ±{fix.HorizontalAccuracy:0} m is worse than the allowed ±{settings.MaxAccuracyMeters:0} m.");
            }

            double? elevation = fix.Altitude;
            var elevationSource = "GPS";
            if (!elevation.HasValue) {
                if (settings.LookupElevationOnline) {
                    progress?.Report("No GPS altitude - looking up terrain elevation…");
                    try {
                        elevation = await LocationService.LookupElevationAsync(fix.Latitude, fix.Longitude, token);
                        elevationSource = "terrain lookup";
                    } catch (Exception ex) when (ex is not OperationCanceledException) {
                        Logger.Warning($"Windows GPS Location: elevation lookup failed: {ex.Message}");
                        elevationSource = "lookup failed, kept current";
                    }
                } else {
                    elevationSource = "no GPS altitude, kept current";
                }
            }

            void Apply() {
                profileService.ChangeLatitude(Math.Round(fix.Latitude, 6));
                profileService.ChangeLongitude(Math.Round(fix.Longitude, 6));
                if (elevation.HasValue) profileService.ChangeElevation(Math.Round(elevation.Value));
            }
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess()) {
                await dispatcher.InvokeAsync(Apply);
            } else {
                Apply();
            }

            var result = new SiteUpdateResult { Fix = fix, Elevation = elevation, ElevationSource = elevationSource };
            Logger.Info($"Windows GPS Location: site set to {result.Summary}");
            return result;
        }
    }
}
