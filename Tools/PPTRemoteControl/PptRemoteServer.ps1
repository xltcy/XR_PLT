param(
    [int]$Port = 3414,
    [int]$DiscoveryPort = 3415,
    [int]$BeaconIntervalSeconds = 1,
    [string]$DiscoveryTarget = "255.255.255.255",
    [int]$StatusIntervalSeconds = 1,
    [ValidateSet("Auto", "PowerPoint", "WPS", "SendKeys")]
    [string]$ControlMode = "Auto"
)

try {
    $udp = [System.Net.Sockets.UdpClient]::new($Port)
    $udp.Client.ReceiveTimeout = 250
}
catch [System.Net.Sockets.SocketException] {
    $endpoint = Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($endpoint) {
        $process = Get-Process -Id $endpoint.OwningProcess -ErrorAction SilentlyContinue
        $processName = if ($process) { $process.ProcessName } else { "unknown" }
        Write-Error "UDP port $Port is already in use by PID $($endpoint.OwningProcess) ($processName). Close that existing PPT remote server window/process, or start this script with another -Port value."
    }
    else {
        Write-Error "UDP port $Port is already in use. Close the existing PPT remote server window/process, or start this script with another -Port value."
    }
    exit 1
}

$remote = [System.Net.IPEndPoint]::new([System.Net.IPAddress]::Any, 0)
$beaconUdp = [System.Net.Sockets.UdpClient]::new()
$beaconUdp.EnableBroadcast = $true
$beaconAddress = [System.Net.IPAddress]::Parse($DiscoveryTarget)
$beaconEndPoint = [System.Net.IPEndPoint]::new($beaconAddress, $DiscoveryPort)
$lastBeaconTime = [DateTime]::MinValue
$lastStatusTime = [DateTime]::MinValue
$lastClientAddress = $null
$lastCommand = $null
$lastCommandTime = $null
$commandCount = 0
$beaconCount = 0
$probeCount = 0
$autoHotkeyExe = $null
$lastStatusTextLength = 0
$automationNotice = ""

Add-Type -AssemblyName System.Windows.Forms

function Get-LocalIPv4Addresses {
    [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
        Where-Object { $_.OperationalStatus -eq [System.Net.NetworkInformation.OperationalStatus]::Up } |
        ForEach-Object { $_.GetIPProperties().UnicastAddresses } |
        Where-Object {
            $_.Address.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork -and
            $_.Address.ToString() -notlike "127.*" -and
            $_.Address.ToString() -notlike "169.254.*"
        } |
        ForEach-Object { $_.Address.ToString() }
}

function U {
    param([int[]]$Codes)
    return -join ($Codes | ForEach-Object { [char]$_ })
}

function Write-ServerStatus {
    $status = if ($lastClientAddress) { U 0x5DF2,0x8FDE,0x63A5 } else { U 0x641C,0x7D22,0x4E2D }
    $lastCommandText = if ($lastCommandTime) { "$lastCommand@$lastClientAddress $($lastCommandTime.ToString('HH:mm:ss'))" } else { U 0x65E0 }
    $automation = if ($autoHotkeyExe) { "AutoHotkey" } else { "$(U 0x5185,0x7F6E,0x6309,0x952E)/COM" }
    $localIpText = (Get-LocalIPv4Addresses) -join ","
    if ([string]::IsNullOrEmpty($localIpText)) {
        $localIpText = U 0x672A,0x83B7,0x53D6
    }

    try {
        [Console]::Clear()
    }
    catch {
    }

    [Console]::WriteLine("$(U 0x72B6,0x6001): $status")
    [Console]::WriteLine("$(U 0x672C,0x673A): $localIpText")
    [Console]::WriteLine("$(U 0x63A7,0x5236)$([char]0x7AEF)$([char]0x53E3): $Port    $(U 0x53D1,0x73B0): $DiscoveryTarget`:$DiscoveryPort")
    [Console]::WriteLine("$(U 0x6A21,0x5F0F): $ControlMode    $(U 0x65B9,0x5F0F): $automation")
    [Console]::WriteLine("$(U 0x5E7F,0x64AD): $beaconCount    $(U 0x63A2,0x6D4B): $probeCount    $(U 0x6307,0x4EE4): $commandCount")
    [Console]::WriteLine("$(U 0x6700,0x540E): $lastCommandText")
    if (-not [string]::IsNullOrEmpty($automationNotice)) {
        [Console]::WriteLine("$(U 0x63D0,0x793A): $automationNotice")
    }
    [Console]::WriteLine("")
    [Console]::WriteLine("Ctrl+C $(U 0x9000,0x51FA)")
}
function Find-AutoHotkey {
    $candidates = @(
        "C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe",
        "C:\Program Files\AutoHotkey\v2\AutoHotkey32.exe",
        "C:\Program Files\AutoHotkey\v2\AutoHotkey.exe",
        "C:\Program Files\AutoHotkey\AutoHotkey64.exe",
        "C:\Program Files\AutoHotkey\AutoHotkey32.exe",
        "C:\Program Files\AutoHotkey\AutoHotkey.exe",
        "C:\Program Files (x86)\AutoHotkey\AutoHotkey.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $command = Get-Command "AutoHotkey.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    return $null
}

function Initialize-AutomationTool {
    if ($ControlMode -ne "WPS" -and $ControlMode -ne "SendKeys") {
        return
    }

    $script:autoHotkeyExe = Find-AutoHotkey
    if (-not $script:autoHotkeyExe) {
        $script:automationNotice = "$(U 0x672A,0x68C0,0x6D4B,0x5230)AutoHotkey"
    }
    else {
        $script:automationNotice = "$(U 0x5DF2,0x68C0,0x6D4B,0x5230)AutoHotkey"
    }
}

function Send-DiscoveryBeacon {
    param([System.Net.IPEndPoint]$TargetEndPoint = $null)

    $message = @{
        service = "XR_PLT_PPT_REMOTE"
        port = $Port
        machine = $env:COMPUTERNAME
        timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    } | ConvertTo-Json -Compress

    $bytes = [Text.Encoding]::UTF8.GetBytes($message)
    $target = if ($TargetEndPoint) { $TargetEndPoint } else { $beaconEndPoint }
    [void]$beaconUdp.Send($bytes, $bytes.Length, $target)
    $script:beaconCount++
}

function Get-ComApplication {
    param([string[]]$ProgIds)

    foreach ($progId in $ProgIds) {
        try {
            $application = [Runtime.InteropServices.Marshal]::GetActiveObject($progId)
            if ($application) {
                return @{ Application = $application; ProgId = $progId }
            }
        }
        catch {
        }
    }

    foreach ($progId in $ProgIds) {
        try {
            $application = New-Object -ComObject $progId
            if ($application) {
                return @{ Application = $application; ProgId = $progId }
            }
        }
        catch {
        }
    }

    return $null
}

function Get-PresentationApplication {
    if ($ControlMode -eq "PowerPoint") {
        return Get-ComApplication @("PowerPoint.Application")
    }

    if ($ControlMode -eq "WPS") {
        return Get-ComApplication @("KWPP.Application", "WPP.Application")
    }

    return Get-ComApplication @("PowerPoint.Application", "KWPP.Application", "WPP.Application")
}

function Get-PowerPointApplication {
    try {
        return [Runtime.InteropServices.Marshal]::GetActiveObject("PowerPoint.Application")
    }
    catch {
        return New-Object -ComObject PowerPoint.Application
    }
}

function Get-SlideShowView {
    param($PowerPoint)

    if ($PowerPoint.SlideShowWindows.Count -gt 0) {
        return $PowerPoint.SlideShowWindows.Item(1).View
    }

    if ($PowerPoint.Presentations.Count -gt 0) {
        $presentation = $PowerPoint.ActivePresentation
        return $presentation.SlideShowSettings.Run().View
    }

    return $null
}

function Invoke-KeyCommand {
    param([string]$Command)

    if ($autoHotkeyExe) {
        $key = switch ($Command.ToLowerInvariant()) {
            "next" { "Right" }
            "previous" { "Left" }
            "prev" { "Left" }
            "first" { "Home" }
            "last" { "End" }
            "end" { "Esc" }
            default { $null }
        }

        if ($key) {
            $ahkScript = Join-Path $env:TEMP "xr_plt_ppt_remote_key.ahk"
            Set-Content -Path $ahkScript -Value "#Requires AutoHotkey v2.0`nSend `"{${key}}`"`n" -Encoding UTF8
            Start-Process -FilePath $autoHotkeyExe -ArgumentList "`"$ahkScript`"" -Wait
            return
        }
    }

    switch ($Command.ToLowerInvariant()) {
        "next" { [System.Windows.Forms.SendKeys]::SendWait("{RIGHT}") }
        "previous" { [System.Windows.Forms.SendKeys]::SendWait("{LEFT}") }
        "prev" { [System.Windows.Forms.SendKeys]::SendWait("{LEFT}") }
        "first" { [System.Windows.Forms.SendKeys]::SendWait("{HOME}") }
        "last" { [System.Windows.Forms.SendKeys]::SendWait("{END}") }
        "end" { [System.Windows.Forms.SendKeys]::SendWait("{ESC}") }
        default { $script:automationNotice = "未知按键指令:$Command" }
    }
}

function Invoke-PptCommand {
    param(
        [string]$Command,
        [int]$SlideNumber = -1
    )

    if ($ControlMode -eq "SendKeys") {
        Invoke-KeyCommand -Command $Command
        return
    }

    $appInfo = Get-PresentationApplication
    if ($appInfo) {
        $ppt = $appInfo.Application
        try {
            $ppt.Visible = -1
        }
        catch {
        }

        $view = Get-SlideShowView -PowerPoint $ppt

        if ($null -ne $view) {
            switch ($Command.ToLowerInvariant()) {
                "next" { $view.Next() }
                "previous" { $view.Previous() }
                "prev" { $view.Previous() }
                "first" { $view.First() }
                "last" { $view.Last() }
                "goto" {
                    if ($SlideNumber -gt 0) {
                        $view.GotoSlide($SlideNumber)
                    }
                }
                "start" { $view.First() }
                "end" { $view.Exit() }
                default { $script:automationNotice = "$(U 0x672A,0x77E5)PPT:$Command" }
            }

            return
        }

        $script:automationNotice = "$($appInfo.ProgId) $(U 0x672A,0x627E,0x5230,0x653E,0x6620,0x7A97,0x53E3)"
    }
    else {
        $script:automationNotice = "$(U 0x672A,0x627E,0x5230)COM"
    }

    Invoke-KeyCommand -Command $Command
}

Initialize-AutomationTool
Write-ServerStatus

while ($true) {
    try {
        if (([DateTime]::UtcNow - $lastBeaconTime).TotalSeconds -ge $BeaconIntervalSeconds) {
            Send-DiscoveryBeacon
            $lastBeaconTime = [DateTime]::UtcNow
        }

        if (([DateTime]::UtcNow - $lastStatusTime).TotalSeconds -ge $StatusIntervalSeconds) {
            Write-ServerStatus
            $lastStatusTime = [DateTime]::UtcNow
        }

        $bytes = $udp.Receive([ref]$remote)
        $json = [Text.Encoding]::UTF8.GetString($bytes)
        $message = $json | ConvertFrom-Json

        $command = if ($message.command) { [string]$message.command } else { "next" }
        if ($message.service -eq "XR_PLT_PPT_REMOTE" -and $command.ToLowerInvariant() -eq "discover") {
            $replyPort = if ($message.replyPort) { [int]$message.replyPort } else { $DiscoveryPort }
            $replyEndPoint = [System.Net.IPEndPoint]::new($remote.Address, $replyPort)
            $lastClientAddress = $remote.Address.ToString()
            $script:probeCount++
            Send-DiscoveryBeacon -TargetEndPoint $replyEndPoint
            Write-ServerStatus
            continue
        }

        $slideNumber = if ($message.slideNumber) { [int]$message.slideNumber } else { -1 }
        $lastClientAddress = $remote.Address.ToString()
        $lastCommand = "$command $slideNumber"
        $lastCommandTime = [DateTime]::Now
        $commandCount++

        Invoke-PptCommand -Command $command -SlideNumber $slideNumber
        Write-ServerStatus
    }
    catch [System.Net.Sockets.SocketException] {
        if ($_.Exception.SocketErrorCode -ne [System.Net.Sockets.SocketError]::TimedOut) {
            $script:automationNotice = "$(U 0x9519,0x8BEF):$($_.Exception.Message)"
            Write-ServerStatus
        }
    }
    catch {
        $script:automationNotice = "$(U 0x9519,0x8BEF):$($_.Exception.Message)"
        Write-ServerStatus
    }
}
