using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NINA.Profile;
using NINA.Profile.Interfaces;

namespace WindowsGpsLocation {

    /// <summary>Plugin options, stored per N.I.N.A. profile.</summary>
    public class PluginSettings : INotifyPropertyChanged {
        public static readonly Guid PluginGuid = Guid.Parse("9c08917d-2ce3-48d1-858e-23e25278489d");

        private readonly PluginOptionsAccessor accessor;

        public PluginSettings(IProfileService profileService) {
            accessor = new PluginOptionsAccessor(profileService, PluginGuid);
        }

        /// <summary>Set the site location automatically when N.I.N.A. starts</summary>
        public bool UpdateOnStartup {
            get => accessor.GetValueBoolean(nameof(UpdateOnStartup), true);
            set { accessor.SetValueBoolean(nameof(UpdateOnStartup), value); RaisePropertyChanged(); }
        }

        /// <summary>How long to wait for a 3D satellite fix before falling back to the best available position</summary>
        public int FixTimeoutSeconds {
            get => accessor.GetValueInt32(nameof(FixTimeoutSeconds), 60);
            set { accessor.SetValueInt32(nameof(FixTimeoutSeconds), Math.Clamp(value, 5, 900)); RaisePropertyChanged(); }
        }

        /// <summary>Reject Wi-Fi/IP positions; only accept a GPS satellite fix</summary>
        public bool RequireSatelliteFix {
            get => accessor.GetValueBoolean(nameof(RequireSatelliteFix), false);
            set { accessor.SetValueBoolean(nameof(RequireSatelliteFix), value); RaisePropertyChanged(); }
        }

        /// <summary>Reject positions whose horizontal accuracy is worse than this</summary>
        public double MaxAccuracyMeters {
            get => accessor.GetValueDouble(nameof(MaxAccuracyMeters), 5000);
            set { accessor.SetValueDouble(nameof(MaxAccuracyMeters), Math.Max(1, value)); RaisePropertyChanged(); }
        }

        /// <summary>When the GPS gives no altitude, look up the ground elevation online (Open-Meteo)</summary>
        public bool LookupElevationOnline {
            get => accessor.GetValueBoolean(nameof(LookupElevationOnline), true);
            set { accessor.SetValueBoolean(nameof(LookupElevationOnline), value); RaisePropertyChanged(); }
        }

        public void RaiseAllChanged() => RaisePropertyChanged(string.Empty);

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
