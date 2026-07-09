$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ServerDir = Resolve-Path (Join-Path $ScriptDir "..\..")
$PidFile = Join-Path $ServerDir "barrage-server.pid"

if (-not (Test-Path $PidFile)) {
    Write-Host "[BarrageServer] pid file not found. Server may not be running."
    exit 0
}

$PidValue = Get-Content $PidFile -ErrorAction SilentlyContinue
if (-not $PidValue) {
    Remove-Item $PidFile -Force
    Write-Host "[BarrageServer] empty pid file removed."
    exit 0
}

$Process = Get-Process -Id $PidValue -ErrorAction SilentlyContinue
if ($Process) {
    Stop-Process -Id $PidValue -Force
    Write-Host "[BarrageServer] stopped. PID=$PidValue"
}
else {
    Write-Host "[BarrageServer] process not found. PID=$PidValue"
}

Remove-Item $PidFile -Force
