#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Creates and configures IIS sites for EventManager API and frontend.

.DESCRIPTION
    Creates two IIS sites:
      - EventManager.Api  on port 5000  → publish/api
      - EventManager.UI   on port 8080  → frontend/EventManagement.UI/dist

    Prerequisites:
      - IIS enabled (Windows Features → Internet Information Services)
      - ASP.NET Core Hosting Bundle installed
      - URL Rewrite Module installed
      - dotnet publish and npm run build already executed

.EXAMPLE
    .\scripts\Setup-IIS.ps1
    .\scripts\Setup-IIS.ps1 -ApiPort 5001 -UiPort 9090
#>

param(
    [Parameter(Mandatory)] [string] $ApiPath,
    [Parameter(Mandatory)] [string] $UiPath,
    [int]    $ApiPort = 5000,
    [int]    $UiPort  = 8080,
    [string] $ApiSite = "EventManager.Api",
    [string] $UiSite  = "EventManager.UI",
    [string] $ApiPool = "EventManager.Api"
)

$ErrorActionPreference = "Stop"

# ── Prerequisites ────────────────────────────────────────────────────────────

if (-not (Get-Module -ListAvailable -Name WebAdministration)) {
    Write-Error "WebAdministration module not found. Ensure IIS is enabled and the ASP.NET Core Hosting Bundle is installed."
}

if (-not (Test-Path $ApiPath)) {
    Write-Error "API artifacts not found at '$ApiPath'."
}

if (-not (Test-Path $UiPath)) {
    Write-Error "Frontend artifacts not found at '$UiPath'."
}

Import-Module WebAdministration

$apiPath = (Resolve-Path $ApiPath).Path
$uiPath  = (Resolve-Path $UiPath).Path

# ── Application Pool — API ───────────────────────────────────────────────────

if (Test-Path "IIS:\AppPools\$ApiPool") {
    Write-Host "[SKIP] App pool '$ApiPool' already exists."
} else {
    New-WebAppPool -Name $ApiPool | Out-Null
    # No Managed Code — ASP.NET Core is self-hosted via ANCM, not Classic .NET CLR
    Set-ItemProperty "IIS:\AppPools\$ApiPool" managedRuntimeVersion ""
    Write-Host "[OK]   App pool '$ApiPool' created (No Managed Code)."
}

# ── IIS Site — API ───────────────────────────────────────────────────────────

if (Test-Path "IIS:\Sites\$ApiSite") {
    Write-Host "[SKIP] Site '$ApiSite' already exists."
} else {
    New-Website -Name $ApiSite `
                -Port $ApiPort `
                -PhysicalPath $apiPath `
                -ApplicationPool $ApiPool | Out-Null
    Write-Host "[OK]   Site '$ApiSite' created on port $ApiPort."
}

# ── IIS Site — Frontend ──────────────────────────────────────────────────────

if (Test-Path "IIS:\Sites\$UiSite") {
    Write-Host "[SKIP] Site '$UiSite' already exists."
} else {
    New-Website -Name $UiSite `
                -Port $UiPort `
                -PhysicalPath $uiPath | Out-Null
    Write-Host "[OK]   Site '$UiSite' created on port $UiPort."
}

# ── Start ────────────────────────────────────────────────────────────────────

Start-Website -Name $ApiSite
Start-Website -Name $UiSite

Write-Host ""
Write-Host "EventManager is running:"
Write-Host "  Frontend  http://localhost:$UiPort"
Write-Host "  API       http://localhost:$ApiPort"
Write-Host "  Health    http://localhost:$ApiPort/health"
