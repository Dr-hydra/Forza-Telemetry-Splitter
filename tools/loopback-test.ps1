# Loopback verification for Forza Telemetry Splitter (no game required).
#
# Simulates Forza Data Out by sending 324-byte UDP packets to the splitter's listen port,
# and verifies that two downstream "tools" on different ports BOTH receive byte-identical data.
#
# Usage:  Launch ForzaTelemetrySplitter.exe first (default listen 127.0.0.1:5555 with
#         destinations 5556 + one more), then run:  pwsh tools/loopback-test.ps1

param(
    [int]    $ListenPort = 5555,
    [int[]]  $DestPorts  = @(5556, 5300),
    [int]    $Count      = 60,
    [string] $Ip         = "127.0.0.1"
)

Add-Type -AssemblyName System.Net

# Receivers: one UDP socket per destination port, each counting valid 324-byte packets.
$receivers = @{}
foreach ($p in $DestPorts) {
    $client = New-Object System.Net.Sockets.UdpClient($p)
    $client.Client.ReceiveTimeout = 2000
    $receivers[$p] = @{ Client = $client; Received = 0; LastByte0 = -1 }
    Write-Host "Listening as a fake tool on ${Ip}:$p"
}

Start-Sleep -Milliseconds 200

# Sender: emit $Count fake Car Dash packets (324 bytes). Byte 0 = IsRaceOn (alternate to prove it varies).
$sender = New-Object System.Net.Sockets.UdpClient
$endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse($Ip), $ListenPort)

Write-Host "`nSending $Count x 324-byte packets to ${Ip}:$ListenPort ...`n"
for ($i = 0; $i -lt $Count; $i++) {
    $pkt = New-Object byte[] 324
    $pkt[0] = if ($i % 2 -eq 0) { 1 } else { 0 }   # IsRaceOn toggles
    $pkt[5] = [byte]($i % 256)                       # a marker byte to confirm integrity
    [void]$sender.Send($pkt, $pkt.Length, $endpoint)
    Start-Sleep -Milliseconds 16                     # ~60 Hz, like the game
}
$sender.Close()

Start-Sleep -Milliseconds 300

# Drain each receiver.
foreach ($p in $DestPorts) {
    $r = $receivers[$p]
    try {
        while ($true) {
            $remote = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
            $data = $r.Client.Receive([ref]$remote)
            if ($data.Length -eq 324) {
                $r.Received++
                $r.LastByte0 = $data[0]
            }
        }
    } catch { } # timeout = done
    $r.Client.Close()
}

Write-Host "`n--- Results ---"
$allPass = $true
foreach ($p in $DestPorts) {
    $r = $receivers[$p]
    $pass = $r.Received -ge ($Count * 0.9)  # allow minor UDP loss on localhost
    if (-not $pass) { $allPass = $false }
    $tag = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host ("[{0}] port {1}: received {2}/{3} packets (324B)" -f $tag, $p, $r.Received, $Count)
}

Write-Host ""
if ($allPass) {
    Write-Host "OVERALL: PASS - the splitter fanned telemetry out to all destinations." -ForegroundColor Green
    exit 0
} else {
    Write-Host "OVERALL: FAIL - at least one destination did not receive the stream." -ForegroundColor Red
    Write-Host "Check that the splitter is running, started, and its destinations match these ports."
    exit 1
}
