[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

$projects = @(
    'src/Identity/PulseGuard.Identity/PulseGuard.Identity.csproj',
    'src/Services/PatientRegistry/PulseGuard.PatientRegistry.Api/PulseGuard.PatientRegistry.Api.csproj',
    'src/Gateways/PulseGuard.DeviceGateway/PulseGuard.DeviceGateway.csproj',
    'src/Services/Telemetry/PulseGuard.Telemetry.Grpc/PulseGuard.Telemetry.Grpc.csproj'
)

$processes = foreach ($project in $projects) {
    $startProcessParameters = @{
        FilePath         = 'dotnet'
        ArgumentList     = @('run', '--project', $project)
        WorkingDirectory = $repositoryRoot
        PassThru         = $true
    }

    Start-Process @startProcessParameters
}

$processes | ForEach-Object {
    Write-Host "Started process $($_.Id): $($_.ProcessName)"
}

Write-Host 'PulseGuard services are starting in separate windows.'
