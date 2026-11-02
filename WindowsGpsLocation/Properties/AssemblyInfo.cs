using System.Reflection;
using System.Runtime.InteropServices;

// [Guid] is the plugin identifier. Keep it stable across releases.
[assembly: Guid("9c08917d-2ce3-48d1-858e-23e25278489d")]

[assembly: AssemblyTitle("Windows GPS Location")]
[assembly: AssemblyDescription("Sets the observing site (latitude, longitude, elevation) from this PC's built-in GPS / Windows location service.")]
[assembly: AssemblyCompany("Louis")]
[assembly: AssemblyProduct("Windows GPS Location")]
[assembly: AssemblyCopyright("Copyright © 2026")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.9001")]

[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "")]
[assembly: AssemblyMetadata("Homepage", "")]
[assembly: AssemblyMetadata("Tags", "GPS,GNSS,Location,Site,Windows")]
[assembly: AssemblyMetadata("ChangelogURL", "")]
[assembly: AssemblyMetadata("FeaturedImageURL", "")]
[assembly: AssemblyMetadata("ScreenshotURL", "")]
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
[assembly: AssemblyMetadata("LongDescription", @"Uses the GPS built into your PC or tablet (anything Windows exposes through its location service, e.g. LTE modems with GNSS such as the Sierra Wireless EM7455) to set N.I.N.A.'s site location.

* Updates latitude, longitude and elevation automatically when N.I.N.A. starts
* 'Get location now' button on the plugin options page
* 'Set location from Windows GPS' sequencer instruction
* Waits for a 3D satellite fix. If the GPS reports no altitude (e.g. only a Wi-Fi position is available) the elevation can be looked up online from Open-Meteo's terrain model, so latitude, longitude and elevation are always set together

Requirements: Windows Settings > Privacy & security > Location must be on, including 'Let desktop apps access your location'.

The location is taken from Windows, not from a COM port, so it can be used alongside other programs that also use the GPS.")]

[assembly: ComVisible(false)]
