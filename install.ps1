# Servitore - Windows Desktop Client Installer
# This script installs the published self-contained WPF client and sets up shortcuts.

param (
    [string]$InstallDir = "C:\Program Files\Servitore",
    [string]$PublishDir = "publish\desktop"
)

# 1. Require Administrator Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Requesting Administrator privileges..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    Exit
}

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "    SERVITORE SERVICE MANAGEMENT INSTALLER   " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Determine path if running from root directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $scriptDir $PublishDir

if (-not (Test-Path $sourceDir)) {
    Write-Error "Source publish directory not found: $sourceDir`nPlease make sure you have run the release build and publish command first."
    Exit
}

# Create installation directory
if (-not (Test-Path $InstallDir)) {
    Write-Host "Creating installation directory: $InstallDir" -ForegroundColor Green
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# Copy files
Write-Host "Copying application files..." -ForegroundColor Green
Copy-Item -Path "$sourceDir\*" -Destination $InstallDir -Recurse -Force

# Create Desktop Shortcut
Write-Host "Creating Desktop shortcut..." -ForegroundColor Green
$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [System.Environment]::GetFolderPath("Desktop")
$Shortcut = $WshShell.CreateShortcut((Join-Path $DesktopPath "Servitore.lnk"))
$Shortcut.TargetPath = Join-Path $InstallDir "Servitore.Desktop.exe"
$Shortcut.WorkingDirectory = $InstallDir
$Shortcut.Description = "Servitore - Service Management System"
$Shortcut.IconLocation = Join-Path $InstallDir "Servitore.Desktop.exe,0"
$Shortcut.Save()

# Create Start Menu Shortcut
Write-Host "Creating Start Menu shortcut..." -ForegroundColor Green
$StartMenuPath = [System.Environment]::GetFolderPath("CommonStartMenu")
$ProgramsPath = Join-Path $StartMenuPath "Programs"
$Shortcut = $WshShell.CreateShortcut((Join-Path $ProgramsPath "Servitore.lnk"))
$Shortcut.TargetPath = Join-Path $InstallDir "Servitore.Desktop.exe"
$Shortcut.WorkingDirectory = $InstallDir
$Shortcut.Description = "Servitore - Service Management System"
$Shortcut.IconLocation = Join-Path $InstallDir "Servitore.Desktop.exe,0"
$Shortcut.Save()

Write-Host ""
Write-Host "=============================================" -ForegroundColor Gold
Write-Host "    Installation Completed Successfully!    " -ForegroundColor Gold
Write-Host "=============================================" -ForegroundColor Gold
Write-Host "App Folder: $InstallDir"
Write-Host "Shortcuts created on Desktop and Start Menu."
Write-Host ""
Write-Host "Press any key to exit..."
[void][System.Console]::ReadKey($true)
