[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot '.env'
$composeFile = Join-Path $repositoryRoot 'deploy/docker-compose/compose.infrastructure.yaml'

if (-not (Test-Path $environmentFile)) {
    throw 'The .env file is missing. Run eng/initialize-development.ps1 first.'
}

& docker compose --env-file $environmentFile -f $composeFile up --detach --wait
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to start the PulseGuard infrastructure containers.'
}

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

Write-Host 'PulseGuard infrastructure is healthy and services are starting in separate windows.'
