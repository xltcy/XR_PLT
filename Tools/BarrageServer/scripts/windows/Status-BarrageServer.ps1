$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ServerDir = Resolve-Path (Join-Path $ScriptDir "..\..")
$PidFile = Join-Path $ServerDir "barrage-server.pid"

if (-not (Test-Path $PidFile)) {
    Write-Host "[BarrageServer] stopped."
    exit 0
}

$PidValue = Get-Content $PidFile -ErrorAction SilentlyContinue
$Process = if ($PidValue) { Get-Process -Id $PidValue -ErrorAction SilentlyContinue } else { $null }

if ($Process) {
    Write-Host "[BarrageServer] running. PID=$PidValue"
}
else {
    Write-Host "[BarrageServer] pid file exists, but process is not running. PID=$PidValue"
}
