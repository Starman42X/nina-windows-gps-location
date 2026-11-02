using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Devices.Geolocation;
using Windows.Foundation;

namespace WindowsGpsLocation {

    public class LocationFix {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public double? Altitude { get; init; }
        public double HorizontalAccuracy { get; init; }
        public PositionSource Source { get; init; }
        public DateTimeOffset Timestamp { get; init; }

        public bool IsSatellite => Source == PositionSource.Satellite;
        public bool Is3D => Altitude.HasValue;
    }

    /// <summary>Reads the position from the Windows location service (Windows.Devices.Geolocation).</summary>
    public static class LocationService {
        private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        /// <summary>
        /// Waits up to <paramref name="timeout"/> for a 3D satellite fix and returns it as soon as it arrives.
        /// When none arrives, returns the best position seen in that time (e.g. Wi-Fi), or throws if there was none.
        /// </summary>
        public static async Task<LocationFix> GetFixAsync(TimeSpan timeout, IProgress<string> progress, CancellationToken token) {
            var access = await Geolocator.RequestAccessAsync().AsTask(token);
            if (access != GeolocationAccessStatus.Allowed) {
                throw new InvalidOperationException("Windows location access is off. Turn on Settings > Privacy & security > Location, including 'Let desktop apps access your location'.");
            }

            var locator = new Geolocator { DesiredAccuracy = PositionAccuracy.High, ReportInterval = 1000 };
            var gate = new object();
            Geoposition best = null;
            var satellite3D = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Consider(Geoposition position) {
                if (position?.Coordinate == null) return;
                lock (gate) {
                    if (IsBetter(position, best)) best = position;
                }
                if (position.Coordinate.PositionSource == PositionSource.Satellite && HasAltitude(position.Coordinate)) {
                    satellite3D.TrySetResult(true);
                }
            }

            TypedEventHandler<Geolocator, PositionChangedEventArgs> onChanged = (_, e) => Consider(e.Position);
            locator.PositionChanged += onChanged;
            try {
                progress?.Report("Waiting for GPS satellite fix…");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(timeout);
                try {
                    Consider(await locator.GetGeopositionAsync(TimeSpan.Zero, timeout).AsTask(cts.Token));
                    await satellite3D.Task.WaitAsync(cts.Token);
                } catch (Exception) when (!token.IsCancellationRequested) {
                    // timed out (or a single read failed) - fall back to the best position seen so far
                }
            } finally {
                locator.PositionChanged -= onChanged;
            }
            token.ThrowIfCancellationRequested();

            Geoposition result;
            lock (gate) { result = best; }
            if (result == null) {
                throw new TimeoutException($"Windows did not report any position within {timeout.TotalSeconds:0} s.");
            }

            var c = result.Coordinate;
            return new LocationFix {
                Latitude = c.Point.Position.Latitude,
                Longitude = c.Point.Position.Longitude,
                Altitude = HasAltitude(c) ? c.Point.Position.Altitude : null,
                HorizontalAccuracy = c.Accuracy,
                Source = c.PositionSource,
                Timestamp = c.Timestamp
            };
        }

        /// <summary>Ground elevation in metres above sea level from Open-Meteo's terrain model (Copernicus DEM, ~90 m grid).</summary>
        public static async Task<double> LookupElevationAsync(double latitude, double longitude, CancellationToken token) {
            var url = string.Format(CultureInfo.InvariantCulture,
                "https://api.open-meteo.com/v1/elevation?latitude={0:0.0000}&longitude={1:0.0000}", latitude, longitude);
            using var response = await http.GetAsync(url, token);
            response.EnsureSuccessStatusCode();
            var json = JObject.Parse(await response.Content.ReadAsStringAsync(token));
            var elevation = json["elevation"]?[0]?.Value<double>() ?? double.NaN;
            if (double.IsNaN(elevation)) throw new InvalidOperationException("Open-Meteo returned no elevation for this position.");
            return elevation;
        }

        // Wi-Fi/IP positions report altitude 0 without an accuracy; a real GNSS altitude comes with one
        private static bool HasAltitude(Geocoordinate c) =>
            c.AltitudeAccuracy.HasValue || (c.PositionSource == PositionSource.Satellite && c.Point.Position.Altitude != 0);

        private static bool IsBetter(Geoposition candidate, Geoposition current) {
            if (current == null) return true;
            var a = candidate.Coordinate;
            var b = current.Coordinate;
            var aSat = a.PositionSource == PositionSource.Satellite;
            var bSat = b.PositionSource == PositionSource.Satellite;
            if (aSat != bSat) return aSat;
            if (HasAltitude(a) != HasAltitude(b)) return HasAltitude(a);
            return a.Accuracy <= b.Accuracy;
        }
    }
}
