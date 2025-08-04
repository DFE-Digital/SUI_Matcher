# SCRIPT: Reinstall-Service.ps1
#
# PURPOSE: Stops, uninstalls, reinstalls, and starts a Windows service.
#
# USAGE: Place this script next to your service executable and run it as an Administrator.
# -------------------------------------------------------------------------------------

# --- Parameters ---
param (
    [Parameter(Mandatory=$true, HelpMessage="Input folder path for the service")]
    [string]$InputPath,
    
    [Parameter(Mandatory=$true, HelpMessage="Output folder path for the service")]
    [string]$Output,
    
    [Parameter(Mandatory=$true, HelpMessage="URI for the service")]
    [string]$Uri,
    
    [Parameter(HelpMessage="Enable gender processing")]
    [switch]$EnableGender
)

# --- Configuration ---
$serviceName = "SUI.Client.Service.Watcher"
$serviceDisplayName = "SUI Client Service Watcher"
$serviceExecutableName = "suiws.exe"

try {
    # Get the script's location to find the executable
   # $scriptPath = Join-Path (Split-Path $PSScriptRoot -Parent) -ChildPath "lib\net9.0\"
    $scriptPath = $PSScriptRoot
    $executablePath = Join-Path $scriptPath $serviceExecutableName

    # 1. STOP & UNINSTALL EXISTING SERVICE
    Write-Host "Checking for existing service '$serviceName'..."
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

    if ($null -ne $service) {
        if ($service.Status -eq 'Running') {
            Write-Host "Service is running. Stopping..."
            Stop-Service -Name $serviceName -Force
            Start-Sleep -Seconds 5
        }
        
        Write-Host "Uninstalling service..."
        sc.exe delete $serviceName
        Write-Host "Old service removed."
    }
    else {
        Write-Host "Service not found. Skipping uninstall."
    }

    # Construct the service parameters
    $serviceParameters = "--input `"$InputPath`" --output `"$Output`" --uri `"$Uri`""
    if ($EnableGender) {
        $serviceParameters += " --enable-gender"
    }

    # 2. INSTALL NEW SERVICE
    Write-Host "Installing new service from '$executablePath'..."
    if (-not (Test-Path $executablePath)) {
        throw "Service executable not found at: $executablePath"
    }

    New-Service -Name $serviceName `
                -BinaryPathName "$executablePath $serviceParameters" `
                -DisplayName $serviceDisplayName `
                -StartupType Automatic

    # 3. CONFIGURE SERVICE RECOVERY
    Write-Host "Configuring service recovery options..."
    sc.exe failure $serviceName reset=60000 actions=restart/60000/restart/600000/none/0
    
    # 4. START NEW SERVICE
    Write-Host "Starting service..."
    Start-Service -Name $serviceName

    Write-Host ""
    Write-Host "Process complete. '$serviceName' has been reinstalled and started."
}
catch {
    Write-Error "An error occurred: $_"
    # Exit with a non-zero status code to indicate failure
    exit 1
}