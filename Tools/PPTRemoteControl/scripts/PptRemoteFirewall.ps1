param(
    [ValidateSet("Menu", "Status", "Open", "Close")]
    [string]$Action = "Status",
    [int]$Port = 3414,
    [string]$RuleName = "XR_PLT_PPT_REMOTE_UDP_3414",
    [string]$RuleDisplayName = "XR_PLT PPT Remote UDP 3414"
)

$LogPath = Join-Path $PSScriptRoot "PptRemoteFirewallStatus.log"

function Write-Log {
    param([string]$Text)

    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $Text"
    Write-Host $line
    Add-Content -Path $LogPath -Value $line -Encoding UTF8
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function U {
    param([int[]]$Codes)
    return -join ($Codes | ForEach-Object { [char]$_ })
}

function Write-Menu {
    Clear-Host
    Write-Host "=========================================="
    Write-Host "XR_PLT PPT $(U 0x8FDC,0x7A0B,0x63A7,0x5236,0x9632,0x706B,0x5899,0x83DC,0x5355)"
    Write-Host "UDP $(U 0x7AEF,0x53E3): $Port"
    Write-Host "=========================================="
    Write-Host "1. $(U 0x68C0,0x6D4B,0x9632,0x706B,0x5899,0x548C,0x7AEF,0x53E3,0x76D1,0x542C,0x72B6,0x6001)"
    Write-Host "2. $(U 0x5F00,0x653E) UDP $Port $(U 0x5165,0x7AD9,0x7AEF,0x53E3)"
    Write-Host "3. $(U 0x5173,0x95ED,0x672C,0x5DE5,0x5177,0x521B,0x5EFA,0x7684) UDP $Port $(U 0x89C4,0x5219)"
    Write-Host "4. $(U 0x9000,0x51FA)"
    Write-Host ""
}

function Show-Menu {
    while ($true) {
        Write-Menu
        $choice = Read-Host (U 0x8BF7,0x8F93,0x5165,0x6570,0x5B57,0x5E76,0x6309,0x56DE,0x8F66)

        switch ($choice) {
            "1" {
                Write-Status
                Read-Host (U 0x6309,0x56DE,0x8F66,0x952E,0x8FD4,0x56DE,0x83DC,0x5355) | Out-Null
            }
            "2" {
                [void](Open-FirewallRule)
                Write-Status
                Read-Host (U 0x6309,0x56DE,0x8F66,0x952E,0x8FD4,0x56DE,0x83DC,0x5355) | Out-Null
            }
            "3" {
                Close-FirewallRule
                Write-Status
                Read-Host (U 0x6309,0x56DE,0x8F66,0x952E,0x8FD4,0x56DE,0x83DC,0x5355) | Out-Null
            }
            "4" {
                exit 0
            }
            default {
                Write-Host (U 0x8F93,0x5165,0x65E0,0x6548,0xFF0C,0x8BF7,0x91CD,0x65B0,0x9009,0x62E9,0x3002)
                Start-Sleep -Seconds 1
            }
        }
    }
}

function Get-UdpListeners {
    try {
        return @(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue)
    }
    catch {
        Write-Log "Unable to query UDP listeners: $($_.Exception.Message)"
        return @()
    }
}

function Write-FirewallServiceStatus {
    $serviceNames = @("MpsSvc", "BFE", "RpcSs")
    foreach ($serviceName in $serviceNames) {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($service) {
            Write-Log "Service: $serviceName status=$($service.Status) startType=$($service.StartType)"
        }
        else {
            Write-Log "Service: $serviceName not found"
        }
    }
}

function Get-PortFirewallRules {
    $matchedRules = @()

    try {
        $portFilters = @(Get-NetFirewallPortFilter -ErrorAction SilentlyContinue | Where-Object {
            ([string]$_.Protocol) -eq "UDP" -and (@($_.LocalPort) -contains [string]$Port -or @($_.LocalPort) -contains $Port)
        })

        foreach ($portFilter in $portFilters) {
            $rule = Get-NetFirewallRule -Name $portFilter.InstanceID -ErrorAction SilentlyContinue
            if ($rule -and $rule.Direction -eq "Inbound") {
                $matchedRules += $rule
            }
        }
    }
    catch {
        Write-Log "Unable to query firewall rules: $($_.Exception.Message)"
    }

    return $matchedRules
}

function Write-Status {
    Write-Log "=== Firewall status for UDP $Port ==="

    $ownRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
    if ($ownRule) {
        Write-Log "Managed rule: exists, enabled=$($ownRule.Enabled), action=$($ownRule.Action), direction=$($ownRule.Direction)"
    }
    else {
        Write-Log "Managed rule: not found"
    }

    $portRules = @(Get-PortFirewallRules)
    if ($ownRule -and -not ($portRules | Where-Object { $_.Name -eq $ownRule.Name })) {
        $portRules += $ownRule
    }
    if ($portRules.Count -gt 0) {
        Write-Log "Inbound UDP $Port rule count: $($portRules.Count)"
        foreach ($rule in $portRules) {
            Write-Log "Rule: name=$($rule.Name), display=$($rule.DisplayName), enabled=$($rule.Enabled), action=$($rule.Action)"
        }
    }
    else {
        Write-Log "Inbound UDP $Port rule count: 0"
    }

    $listeners = @(Get-UdpListeners)
    if ($listeners.Count -gt 0) {
        Write-Log "UDP $Port listener count: $($listeners.Count)"
        foreach ($listener in $listeners) {
            $process = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
            $processName = if ($process) { $process.ProcessName } else { "unknown" }
            Write-Log "Listener: address=$($listener.LocalAddress), pid=$($listener.OwningProcess), process=$processName"
        }
    }
    else {
        Write-Log "UDP $Port listener count: 0"
    }

    Write-Log "Log file: $LogPath"
}

function Open-FirewallRule {
    if (-not (Test-IsAdministrator)) {
        Write-Log "Administrator permission is required to open UDP $Port."
        Write-Log "Right-click the PptRemoteFirewallMenu.bat file and choose Run as administrator."
        exit 1
    }

    Write-FirewallServiceStatus

    $firewallService = Get-Service -Name "MpsSvc" -ErrorAction SilentlyContinue
    if ($firewallService -and $firewallService.Status -ne "Running") {
        Write-Log "Windows Defender Firewall service (MpsSvc) is not running. Current status=$($firewallService.Status), startType=$($firewallService.StartType)."
        Write-Log "Enable and start MpsSvc first, then open UDP $Port again."
    }

    $existingRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
    if ($existingRule) {
        try {
            Remove-NetFirewallRule -Name $RuleName -ErrorAction Stop
            Write-Log "Removed old managed rule: $RuleName"
        }
        catch {
            Write-Log "Failed to remove old managed rule: $($_.Exception.Message)"
            return $false
        }
    }

    try {
        New-NetFirewallRule `
            -Name $RuleName `
            -DisplayName $RuleDisplayName `
            -Direction Inbound `
            -Action Allow `
            -Protocol UDP `
            -LocalPort $Port `
            -Profile Any `
            -Enabled True `
            -ErrorAction Stop | Out-Null

        $createdRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
        if ($createdRule) {
            Write-Log "Opened inbound UDP $Port with rule: $RuleDisplayName"
            return $true
        }

        Write-Log "Failed to verify created firewall rule: $RuleName"
        return $false
    }
    catch {
        Write-Log "Failed to open inbound UDP ${Port}: $($_.Exception.Message)"
        Write-Log "If the error is 1753, check Windows Defender Firewall, Base Filtering Engine, and RPC services."
        Write-Log "You can also open Windows Defender Firewall manually and allow inbound UDP $Port."
        return $false
    }
}

function Close-FirewallRule {
    if (-not (Test-IsAdministrator)) {
        Write-Log "Administrator permission is required to close UDP $Port."
        Write-Log "Right-click the PptRemoteFirewallMenu.bat file and choose Run as administrator."
        exit 1
    }

    Write-FirewallServiceStatus

    $existingRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
    if ($existingRule) {
        try {
            Remove-NetFirewallRule -Name $RuleName -ErrorAction Stop
            Write-Log "Removed managed rule: $RuleName"
        }
        catch {
            Write-Log "Failed to remove managed rule: $($_.Exception.Message)"
        }
    }
    else {
        Write-Log "Managed rule was not found. Nothing to remove."
    }
}

Write-Log "Action=$Action, Port=$Port"

switch ($Action) {
    "Menu" {
        Show-Menu
    }
    "Open" {
        Open-FirewallRule
        Write-Status
    }
    "Close" {
        Close-FirewallRule
        Write-Status
    }
    default {
        Write-Status
    }
}
