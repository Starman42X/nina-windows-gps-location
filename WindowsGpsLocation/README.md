# Windows GPS Location – N.I.N.A. plugin

Sets N.I.N.A.'s site location (latitude, longitude, elevation) from the GPS built into your PC,
via the Windows location service. Works with any GNSS that Windows exposes as a location sensor,
e.g. LTE modems with GPS like the Sierra Wireless EM7455 in many laptops and tablets.

N.I.N.A. itself only supports NMEA serial GPS receivers and gpsd. This plugin fills the gap.

## Features

- Sets the location automatically when N.I.N.A. starts (and when you switch profiles)
- **Get location now** button under *Options → Plugins → Windows GPS Location*
- **Set location from Windows GPS** sequencer instruction (category *Utility*)
- Waits for a 3D satellite fix (default 60 s). If none arrives, it uses the best position Windows has
  (e.g. Wi-Fi), and looks up the ground elevation from Open-Meteo's terrain model, so latitude,
  longitude and elevation are always set together. Both behaviours can be turned off.

If *Options → General → Telescope → Location sync* is set to "To telescope", N.I.N.A. sends the
updated location to the mount when the mount connects.

## Requirements

- N.I.N.A. 3.2 or newer (64-bit)
- Windows 10/11 with *Settings → Privacy & security → Location* on, including
  **Let desktop apps access your location**
- For a real satellite fix: the GPS antenna connected and a clear view of the sky

## Install

1. Close N.I.N.A.
2. Unzip so that the folder `Windows GPS Location` ends up in
   `%LOCALAPPDATA%\NINA\Plugins\3.0.0\`
3. Start N.I.N.A. The plugin is listed under *Options → Plugins*.

## Options

| Option | Default | |
|---|---|---|
| Set location when N.I.N.A. starts | on | |
| Wait for 3D satellite fix (seconds) | 60 | Falls back to the best available position after this |
| Require GPS satellite fix | off | When on, Wi-Fi/IP positions are rejected |
| Reject positions less accurate than (m) | 5000 | |
| Look up elevation online if GPS has none | on | Sends the position (rounded to ~10 m) to `api.open-meteo.com`. Off: the current elevation is kept |

## Build

```
dotnet build -c Release
```

Requires the .NET 8 SDK. Ship `NINA.Plugin.WindowsGpsLocation.dll`, `Microsoft.Windows.SDK.NET.dll`
and `WinRT.Runtime.dll` from the build output.

Licence: MPL-2.0
