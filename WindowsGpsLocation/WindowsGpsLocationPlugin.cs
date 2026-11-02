using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Utility;
using RelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;
using NINA.Core.Utility.Notification;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;

namespace WindowsGpsLocation {

    [Export(typeof(IPluginManifest))]
    public class WindowsGpsLocationPlugin : PluginBase, INotifyPropertyChanged {
        private readonly IProfileService profileService;
        private CancellationTokenSource updateCts;
        private bool isUpdating;
        private string status = "Not run yet";

        [ImportingConstructor]
        public WindowsGpsLocationPlugin(IProfileService profileService) {
            this.profileService = profileService;
            Settings = new PluginSettings(profileService);
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            UpdateNowCommand = new AsyncRelayCommand(() => UpdateLocationAsync(showSuccess: true));
            CancelCommand = new RelayCommand(() => updateCts?.Cancel());
        }

        public PluginSettings Settings { get; }

        public ICommand UpdateNowCommand { get; }
        public ICommand CancelCommand { get; }

        public bool IsUpdating {
            get => isUpdating;
            private set { isUpdating = value; RaisePropertyChanged(); }
        }

        public string Status {
            get => status;
            private set { status = value; RaisePropertyChanged(); }
        }

        public override Task Initialize() {
            if (Settings.UpdateOnStartup) {
                _ = UpdateLocationAsync(showSuccess: true);
            }
            return Task.CompletedTask;
        }

        public override Task Teardown() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            updateCts?.Cancel();
            return base.Teardown();
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            Settings.RaiseAllChanged();
            if (Settings.UpdateOnStartup && !IsUpdating) {
                _ = UpdateLocationAsync(showSuccess: true);
            }
        }

        private async Task UpdateLocationAsync(bool showSuccess) {
            if (IsUpdating) return;
            IsUpdating = true;
            updateCts = new CancellationTokenSource();
            try {
                var progress = new Progress<string>(s => Status = s);
                var result = await SiteUpdater.UpdateSiteAsync(profileService, Settings, progress, updateCts.Token);
                Status = $"Set {DateTime.Now:HH:mm:ss}: {result.Summary}";
                if (showSuccess) Notification.ShowSuccess($"Site location set from Windows GPS:\n{result.Summary}");
            } catch (OperationCanceledException) {
                Status = "Cancelled";
            } catch (Exception ex) {
                Logger.Error("Windows GPS Location: could not set site location", ex);
                Status = $"Failed {DateTime.Now:HH:mm:ss}: {ex.Message}";
                Notification.ShowWarning($"Windows GPS Location: {ex.Message}");
            } finally {
                updateCts.Dispose();
                updateCts = null;
                IsUpdating = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
