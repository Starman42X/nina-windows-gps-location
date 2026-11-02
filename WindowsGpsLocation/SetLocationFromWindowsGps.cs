using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;

namespace WindowsGpsLocation {

    [ExportMetadata("Name", "Set location from Windows GPS")]
    [ExportMetadata("Description", "Waits for a GPS fix from the Windows location service and sets the site latitude, longitude and elevation.")]
    [ExportMetadata("Icon", "WindowsGpsLocation_PinSVG")]
    [ExportMetadata("Category", "Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetLocationFromWindowsGps : SequenceItem {
        private readonly IProfileService profileService;

        [ImportingConstructor]
        public SetLocationFromWindowsGps(IProfileService profileService) {
            this.profileService = profileService;
        }

        private SetLocationFromWindowsGps(SetLocationFromWindowsGps cloneMe) : this(cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() => new SetLocationFromWindowsGps(this);

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var status = new Progress<string>(s => progress?.Report(new ApplicationStatus { Status = s }));
            await SiteUpdater.UpdateSiteAsync(profileService, new PluginSettings(profileService), status, token);
        }

        public override TimeSpan GetEstimatedDuration() => TimeSpan.FromSeconds(new PluginSettings(profileService).FixTimeoutSeconds);

        public override string ToString() => $"Category: {Category}, Item: {nameof(SetLocationFromWindowsGps)}";
    }
}
