[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$identityProject = Join-Path $repositoryRoot 'src/Identity/PulseGuard.Identity/PulseGuard.Identity.csproj'
$patientRegistryProject = Join-Path $repositoryRoot 'src/Services/PatientRegistry/PulseGuard.PatientRegistry.Api/PulseGuard.PatientRegistry.Api.csproj'
$environmentFile = Join-Path $repositoryRoot '.env'

function New-DevelopmentSecret {
    $secretBytes = [byte[]]::new(32)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($secretBytes)
    return [Convert]::ToBase64String($secretBytes)
}

function Get-OrCreateEnvironmentSecret {
    param(
        [Parameter(Mandatory)]
        [string] $Name
    )

    if (Test-Path $environmentFile) {
        $match = Select-String -Path $environmentFile -Pattern "^$([regex]::Escape($Name))=(.+)$" |
            Select-Object -First 1

        if ($null -ne $match) {
            return $match.Matches[0].Groups[1].Value
        }
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

@(
    "POSTGRES_PASSWORD=$postgresPassword"
    'RABBITMQ_DEFAULT_USER=pulseguard'
    "RABBITMQ_DEFAULT_PASS=$rabbitMqPassword"
) | Set-Content -Path $environmentFile -Encoding utf8NoBOM

Write-Host 'Development configuration initialized.'
Write-Host 'PostgreSQL and RabbitMQ credentials were generated and stored in the ignored .env file.'
Write-Host 'Copy the following one-time value into the Postman clientSecret variable:'
Write-Host $clientSecret
