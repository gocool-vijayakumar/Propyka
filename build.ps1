# Propyka build script
#
# Builds the ASP.NET Core API and the Angular web app into .\dist
#   dist\api   - published API (run with: dotnet dist\api\Propyka.Api.dll)
#   dist\web   - production web app (static files, any static host)
#
# Usage (from the repo root, in PowerShell or Command Prompt):
#   .\build.ps1                    full build, runs web unit tests
#   .\build.ps1 -SkipTests         skip the web unit tests
#   .\build.ps1 -SkipApi           build the web app only
#   .\build.ps1 -SkipWeb           build the API only
#   .\build.ps1 -CleanWeb          reinstall web packages (npm ci) before building
#   .\build.ps1 -Configuration Debug

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SkipApi,
    [switch]$SkipWeb,
    [switch]$SkipTests,
    [switch]$CleanWeb
)

$ErrorActionPreference = 'Stop'

$root     = $PSScriptRoot
$dist     = Join-Path $root 'dist'
$apiProj  = Join-Path $root 'backend\Propyka\Propyka.Api\Propyka.Api.csproj'
$webDir   = Join-Path $root 'frontend\propyka-web'
$prodEnv  = Join-Path $webDir 'src\environments\environment.production.ts'
$timer    = [System.Diagnostics.Stopwatch]::StartNew()

function Write-Step([string]$Text) {
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

function Assert-Command([string]$Name, [string]$Hint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "'$Name' was not found on PATH. $Hint"
    }
}

function Assert-ExitCode([string]$Step) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed (exit code $LASTEXITCODE)."
    }
}

# ---------------------------------------------------------------------------
# Preflight
# ---------------------------------------------------------------------------

Write-Step 'Checking tools'
if (-not $SkipApi) {
    Assert-Command 'dotnet' 'Install the .NET 8 SDK from https://dotnet.microsoft.com/download'
    Write-Host ("    dotnet {0}" -f (dotnet --version))
}
if (-not $SkipWeb) {
    Assert-Command 'node' 'Install Node.js 20 or newer from https://nodejs.org'
    Assert-Command 'npm'  'npm ships with Node.js. Reinstall Node.js if it is missing.'
    Write-Host ("    node {0}, npm {1}" -f (node --version), (npm --version))
}

if (-not $SkipWeb -and (Test-Path $prodEnv)) {
    $envText = Get-Content $prodEnv -Raw
    if ($envText -match 'YOUR-API-HOST') {
        Write-Warning 'frontend/propyka-web/src/environments/environment.production.ts still has the placeholder API address. Set apiUrl to your deployed API before publishing the web build.'
    }
}

if (Test-Path $dist) {
    Write-Step 'Cleaning previous output'
    Remove-Item $dist -Recurse -Force
}
New-Item -ItemType Directory -Path $dist | Out-Null

# ---------------------------------------------------------------------------
# API
# ---------------------------------------------------------------------------

if (-not $SkipApi) {
    Write-Step "Restoring API packages"
    dotnet restore $apiProj
    Assert-ExitCode 'dotnet restore'

    Write-Step "Building API ($Configuration)"
    dotnet build $apiProj -c $Configuration --no-restore
    Assert-ExitCode 'dotnet build'

    Write-Step 'Publishing API to dist\api'
    dotnet publish $apiProj -c $Configuration --no-restore --no-build -o (Join-Path $dist 'api')
    Assert-ExitCode 'dotnet publish'
}

# ---------------------------------------------------------------------------
# Web
# ---------------------------------------------------------------------------

if (-not $SkipWeb) {
    Push-Location $webDir
    try {
        if ($CleanWeb -or -not (Test-Path 'node_modules')) {
            Write-Step 'Installing web packages (npm ci)'
            npm ci
            Assert-ExitCode 'npm ci'
        }

        if (-not $SkipTests) {
            Write-Step 'Running web unit tests'
            npx ng test --watch=false
            Assert-ExitCode 'Web unit tests'
        }

        Write-Step 'Building web app (production)'
        npx ng build --configuration production
        Assert-ExitCode 'ng build'
    }
    finally {
        Pop-Location
    }

    $built = Join-Path $webDir 'dist\propyka-web\browser'
    if (-not (Test-Path $built)) {
        $built = Join-Path $webDir 'dist\propyka-web'
    }
    if (-not (Test-Path $built)) {
        throw "Web build output was not found under $webDir\dist."
    }

    Write-Step 'Copying web app to dist\web'
    Copy-Item -Path $built -Destination (Join-Path $dist 'web') -Recurse -Force
}

# ---------------------------------------------------------------------------
# Build info
# ---------------------------------------------------------------------------

$commit = 'unknown'
if (Get-Command git -ErrorAction SilentlyContinue) {
    $found = git -C $root rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and $found) { $commit = $found.Trim() }
}

$info = @(
    "Propyka build",
    "Built at : $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))",
    "Commit   : $commit",
    "Config   : $Configuration",
    "API      : $(if ($SkipApi) { 'skipped' } else { 'dist\api' })",
    "Web      : $(if ($SkipWeb) { 'skipped' } else { 'dist\web' })"
)
Set-Content -Path (Join-Path $dist 'BUILD-INFO.txt') -Value $info

$timer.Stop()
Write-Host ''
Write-Host ("Build succeeded in {0:N0} seconds." -f $timer.Elapsed.TotalSeconds) -ForegroundColor Green
Write-Host "Output: $dist"
