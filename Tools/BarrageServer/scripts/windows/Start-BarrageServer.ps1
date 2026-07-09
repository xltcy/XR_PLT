param(
    [int]$Port = 37621,
    [string]$PublicBaseUrl = "",
    [string]$NodePath = "node",
    [string]$NpmPath = "npm",
    [switch]$InstallDependencies
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Resolve-BarrageServerDir {
    param([string]$StartDir)

    $CheckedDirs = New-Object System.Collections.Generic.List[string]

    # Search upward from the script directory until package.json and server.js
    # are found. Keep this script ASCII-only so Windows PowerShell 5.1 will not
    # break execution when the file is copied with a non-UTF8 encoding.
    $CurrentDir = [System.IO.Path]::GetFullPath($StartDir)
    while (-not [string]::IsNullOrWhiteSpace($CurrentDir)) {
        $CheckedDirs.Add($CurrentDir)

        $PackageJson = Join-Path $CurrentDir "package.json"
        $ServerJs = Join-Path $CurrentDir "server.js"
        if ((Test-Path $PackageJson) -and (Test-Path $ServerJs)) {
            return $CurrentDir
        }

        $ParentDir = Split-Path -Parent $CurrentDir
        if ([string]::IsNullOrWhiteSpace($ParentDir) -or $ParentDir -eq $CurrentDir) {
            break
        }

        $CurrentDir = $ParentDir
    }

    $MessageLines = New-Object System.Collections.Generic.List[string]
    $MessageLines.Add("Cannot find BarrageServer root.")
    $MessageLines.Add("Please copy and run the whole Tools/BarrageServer directory, not only the scripts/windows folder.")
    $MessageLines.Add("")
    $MessageLines.Add("A valid BarrageServer root must contain both files:")
    $MessageLines.Add("  - package.json")
    $MessageLines.Add("  - server.js")
    $MessageLines.Add("")
    $MessageLines.Add("Checked directories:")
    foreach ($Dir in $CheckedDirs) {
        $HasPackageJson = Test-Path (Join-Path $Dir "package.json")
        $HasServerJs = Test-Path (Join-Path $Dir "server.js")
        $MessageLines.Add("  - $Dir  package.json=$HasPackageJson  server.js=$HasServerJs")
    }

    throw ($MessageLines -join [Environment]::NewLine)
}

$ServerDir = Resolve-BarrageServerDir $ScriptDir
$LogDir = Join-Path $ServerDir "logs"
$PidFile = Join-Path $ServerDir "barrage-server.pid"
$OutLog = Join-Path $LogDir "server.out.log"
$ErrLog = Join-Path $LogDir "server.err.log"

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Test-CommandExists {
    param([string]$CommandName)
    return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

if (-not (Test-CommandExists $NodePath)) {
    Write-Host "[BarrageServer] node was not found."
    Write-Host "[BarrageServer] Please install Node.js LTS, then reopen this window and run again."
    Write-Host "[BarrageServer] Download: https://nodejs.org/"
    exit 1
}

if (-not (Test-CommandExists $NpmPath)) {
    Write-Host "[BarrageServer] npm was not found."
    Write-Host "[BarrageServer] npm is installed with Node.js. Please install Node.js LTS or add npm to PATH."
    Write-Host "[BarrageServer] Download: https://nodejs.org/"
    exit 1
}

if (Test-Path $PidFile) {
    $OldPid = Get-Content $PidFile -ErrorAction SilentlyContinue
    if ($OldPid) {
        $OldProcess = Get-Process -Id $OldPid -ErrorAction SilentlyContinue
        if ($OldProcess) {
            Write-Host "[BarrageServer] already running. PID=$OldPid"
            Write-Host "[BarrageServer] page: http://<server-host>:$Port/s/default"
            exit 0
        }
    }
}

if ($InstallDependencies -or -not (Test-Path (Join-Path $ServerDir "node_modules"))) {
    Write-Host "[BarrageServer] installing npm dependencies..."
    Push-Location $ServerDir
    & $NpmPath install
    if ($LASTEXITCODE -ne 0) {
        Pop-Location
        throw "npm install failed with exit code $LASTEXITCODE."
    }
    Pop-Location
}

$env:PORT = [string]$Port
$env:PUBLIC_BASE_URL = $PublicBaseUrl
$env:NODE_ENV = "production"

$Process = Start-Process `
    -FilePath $NodePath `
    -ArgumentList "server.js" `
    -WorkingDirectory $ServerDir `
    -WindowStyle Hidden `
    -RedirectStandardOutput $OutLog `
    -RedirectStandardError $ErrLog `
    -PassThru

$Process.Id | Set-Content -Path $PidFile -Encoding ASCII

Write-Host "[BarrageServer] started. PID=$($Process.Id)"
if ([string]::IsNullOrWhiteSpace($PublicBaseUrl)) {
    Write-Host "[BarrageServer] page: http://<server-host>:$Port/s/default"
    Write-Host "[BarrageServer] health: http://<server-host>:$Port/health"
}
else {
    $BaseUrl = $PublicBaseUrl.TrimEnd("/")
    Write-Host "[BarrageServer] page: $BaseUrl/s/default"
    Write-Host "[BarrageServer] health: $BaseUrl/health"
}
Write-Host "[BarrageServer] logs: $LogDir"
