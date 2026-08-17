[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$identityProject = Join-Path $repositoryRoot 'src/Identity/PulseGuard.Identity/PulseGuard.Identity.csproj'
$deviceGatewayProject = Join-Path $repositoryRoot 'src/Gateways/PulseGuard.DeviceGateway/PulseGuard.DeviceGateway.csproj'
$patientRegistryProject = Join-Path $repositoryRoot 'src/Services/PatientRegistry/PulseGuard.PatientRegistry.Api/PulseGuard.PatientRegistry.Api.csproj'
$telemetryProject = Join-Path $repositoryRoot 'src/Services/Telemetry/PulseGuard.Telemetry.Grpc/PulseGuard.Telemetry.Grpc.csproj'
$environmentFile = Join-Path $repositoryRoot '.env'
$secretFunctions = Join-Path $PSScriptRoot 'functions/development-secrets.ps1'

. $secretFunctions

function Get-OrCreateEnvironmentSecret {
    param(
        [Parameter(Mandatory)]
        [string] $Name
    )

    $existingSecret = Get-EnvironmentFileValue -Path $environmentFile -Name $Name
    if (-not [string]::IsNullOrWhiteSpace($existingSecret)) {
        return $existingSecret
    }

    return New-DevelopmentSecret
}

$clientSecret = New-DevelopmentSecret
$postgresPassword = Get-OrCreateEnvironmentSecret -Name 'POSTGRES_PASSWORD'
$rabbitMqPassword = Get-OrCreateEnvironmentSecret -Name 'RABBITMQ_DEFAULT_PASS'
$patientRegistryConnectionString = "Host=localhost;Port=5432;Database=pulseguard;Username=pulseguard;Password=$postgresPassword"

& dotnet user-secrets set 'Clients:Postman:ClientSecret' $clientSecret --project $identityProject
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to store the Postman client secret with .NET User Secrets.'
}

& dotnet user-secrets set 'ConnectionStrings:PatientRegistry' $patientRegistryConnectionString --project $patientRegistryProject
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to store the Patient Registry connection string with .NET User Secrets.'
}

& dotnet user-secrets set 'ConnectionStrings:Telemetry' $patientRegistryConnectionString --project $telemetryProject
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to store the Telemetry connection string with .NET User Secrets.'
}

& dotnet user-secrets set 'Messaging:RabbitMq:Password' $rabbitMqPassword --project $deviceGatewayProject
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to store the Device Gateway RabbitMQ password with .NET User Secrets.'
}

& dotnet user-secrets set 'Messaging:RabbitMq:Password' $rabbitMqPassword --project $patientRegistryProject
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to store the Patient Registry RabbitMQ password with .NET User Secrets.'
}

$environmentValues = @(
    "POSTGRES_PASSWORD=$postgresPassword"
    'RABBITMQ_DEFAULT_USER=pulseguard'
    "RABBITMQ_DEFAULT_PASS=$rabbitMqPassword"
)

Set-Utf8NoBomContent -Path $environmentFile -Value $environmentValues

Write-Host 'Development configuration initialized.'
Write-Host 'PostgreSQL and RabbitMQ credentials were generated and stored in the ignored .env file.'

if ($null -ne (Get-Command 'Set-Clipboard' -ErrorAction SilentlyContinue)) {
    Set-Clipboard -Value $clientSecret
    Write-Host 'The Postman client secret was copied to the Windows clipboard.'
}
else {
    Write-Warning 'Set-Clipboard is unavailable. The Postman client secret remains stored in .NET User Secrets and was not displayed.'
}
