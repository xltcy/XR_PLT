param(
    [ValidateSet("Status", "Open", "Close")]
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

function Get-UdpListeners {
    try {
        return @(Get-NetUDPEndpoint -LocalPort $Port -ErrorAction SilentlyContinue)
    }
    catch {
        Write-Log "Unable to query UDP listeners: $($_.Exception.Message)"
        return @()
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

    $existingRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
    if ($existingRule) {
        Remove-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
        Write-Log "Removed old managed rule: $RuleName"
    }

    New-NetFirewallRule `
        -Name $RuleName `
        -DisplayName $RuleDisplayName `
        -Direction Inbound `
        -Action Allow `
        -Protocol UDP `
        -LocalPort $Port `
        -Profile Any `
        -Enabled True | Out-Null

    Write-Log "Opened inbound UDP $Port with rule: $RuleDisplayName"
}

function Close-FirewallRule {
    if (-not (Test-IsAdministrator)) {
        Write-Log "Administrator permission is required to close UDP $Port."
        Write-Log "Right-click the PptRemoteFirewallMenu.bat file and choose Run as administrator."
        exit 1
    }

    $existingRule = Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
    if ($existingRule) {
        Remove-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue
        Write-Log "Removed managed rule: $RuleName"
    }
    else {
        Write-Log "Managed rule was not found. Nothing to remove."
    }
}

Write-Log "Action=$Action, Port=$Port"

switch ($Action) {
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
