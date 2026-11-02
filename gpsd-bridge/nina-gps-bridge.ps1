<#
  NINA GPS bridge
  Serves this PC's Windows location (built-in GNSS module, or Wi-Fi/IP fallback)
  as a minimal gpsd server, so N.I.N.A. can use it via
  Options > General > Astrometry > GNSS source "gpsd", host localhost, port 2947.

  When Windows has no GPS altitude (e.g. a Wi-Fi position indoors), the ground elevation is looked up
  from api.open-meteo.com so NINA still gets a 3D fix. Use -NoElevationLookup to disable.

  Usage:  powershell -ExecutionPolicy Bypass -File nina-gps-bridge.ps1 [-Port 2947] [-MaxAccuracyMeters 5000] [-NoElevationLookup]
#>
param(
    [int]$Port = 2947,
    # Fixes worse than this (horizontal accuracy) are reported to NINA as "no fix"
    [double]$MaxAccuracyMeters = 5000,
    # How long a client may stay connected receiving updates
    [int]$SessionSeconds = 30,
    # Don't look up terrain elevation online (api.open-meteo.com) when the GPS reports no altitude
    [switch]$NoElevationLookup
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Device
$inv = [System.Globalization.CultureInfo]::InvariantCulture

$watcher = New-Object System.Device.Location.GeoCoordinateWatcher([System.Device.Location.GeoPositionAccuracy]::High)
$watcher.MovementThreshold = 1
if (-not $watcher.TryStart($false, [TimeSpan]::FromSeconds(10))) {
    Write-Warning "Windows location service did not start yet - check Settings > Privacy & security > Location."
}

function Num([double]$v) { $v.ToString('R', $inv) }

# Terrain elevation for positions without GPS altitude (Wi-Fi/IP fixes), cached per ~100 m cell
$elevationCache = @{}
$lastLookupFailure = [DateTime]::MinValue
function Get-TerrainElevation([double]$lat, [double]$lon) {
    if ($NoElevationLookup) { return $null }
    $key = '{0:0.000},{1:0.000}' -f $lat, $lon
    if ($elevationCache.ContainsKey($key)) { return $elevationCache[$key] }
    if (([DateTime]::UtcNow - $script:lastLookupFailure).TotalSeconds -lt 60) { return $null }
    try {
        $url = [string]::Format($inv, 'https://api.open-meteo.com/v1/elevation?latitude={0:0.0000}&longitude={1:0.0000}', $lat, $lon)
        $elevation = [double](Invoke-RestMethod -Uri $url -TimeoutSec 10).elevation[0]
        $elevationCache[$key] = $elevation
        return $elevation
    } catch {
        $script:lastLookupFailure = [DateTime]::UtcNow
        return $null
    }
}

function Get-Tpv {
    $pos = $watcher.Position
    $loc = $pos.Location
    $time = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", $inv)
    if ($loc.IsUnknown -or [double]::IsNaN($loc.Latitude) -or
        (-not [double]::IsNaN($loc.HorizontalAccuracy) -and $loc.HorizontalAccuracy -gt $MaxAccuracyMeters)) {
        return '{"class":"TPV","device":"windows-location","mode":1,"time":"' + $time + '"}'
    }
    $hasGpsAlt = -not [double]::IsNaN($loc.Altitude) -and $loc.Altitude -ne 0 -and
                 -not [double]::IsNaN($loc.VerticalAccuracy)
    $alt = if ($hasGpsAlt) { $loc.Altitude } else { Get-TerrainElevation $loc.Latitude $loc.Longitude }
    $mode = if ($null -ne $alt) { 3 } else { 2 }
    $json = '{"class":"TPV","device":"windows-location","mode":' + $mode + ',"time":"' + $time + '"' +
            ',"lat":' + (Num $loc.Latitude) + ',"lon":' + (Num $loc.Longitude)
    if ($null -ne $alt) { $json += ',"alt":' + (Num $alt) + ',"altMSL":' + (Num $alt) }
    if (-not [double]::IsNaN($loc.HorizontalAccuracy)) { $json += ',"eph":' + (Num $loc.HorizontalAccuracy) }
    if ($hasGpsAlt) { $json += ',"epv":' + (Num $loc.VerticalAccuracy) }
    return $json + '}'
}

$listener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, $Port)
$listener.Start()
Write-Host "NINA GPS bridge listening on localhost:$Port (Ctrl+C to stop)"

try {
    while ($true) {
        $client = $listener.AcceptTcpClient()
        try {
            $stream = $client.GetStream()
            $stream.ReadTimeout = 1000
            $reader = New-Object System.IO.StreamReader($stream)
            $writer = New-Object System.IO.StreamWriter($stream)
            $writer.NewLine = "`r`n"
            $writer.AutoFlush = $true

            $writer.WriteLine('{"class":"VERSION","release":"3.25","rev":"nina-gps-bridge","proto_major":3,"proto_minor":15}')
            $deadline = [DateTime]::UtcNow.AddSeconds($SessionSeconds)
            $watching = $false
            while ($client.Connected -and [DateTime]::UtcNow -lt $deadline) {
                $timedOut = $false
                try { $line = $reader.ReadLine() } catch [System.IO.IOException] { $line = ''; $timedOut = $true }
                if ($null -eq $line) { break }  # client closed the connection
                if ($line -match '^\?WATCH') {
                    $watching = $true
                    $writer.WriteLine('{"class":"DEVICES","devices":[{"class":"DEVICE","path":"windows-location","driver":"Windows Location API","activated":"' + [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", $inv) + '"}]}')
                    $writer.WriteLine('{"class":"WATCH","enable":true,"json":true,"nmea":false,"raw":0,"scaled":false,"timing":false,"split24":false,"pps":false}')
                    $writer.WriteLine((Get-Tpv))
                } elseif ($line -match '^\?POLL') {
                    $writer.WriteLine('{"class":"POLL","time":"' + [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", $inv) + '","active":1,"tpv":[' + (Get-Tpv) + '],"sky":[]}')
                }
                # one TPV per second while watching (the read timeout paces the loop)
                if ($watching -and $timedOut) { $writer.WriteLine((Get-Tpv)) }
            }
        } catch {
            # client went away; wait for the next one
        } finally {
            $client.Close()
        }
    }
} finally {
    $listener.Stop()
    $watcher.Stop()
}
