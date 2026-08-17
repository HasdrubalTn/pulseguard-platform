[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot '.env'
$secretFunctions = Join-Path $PSScriptRoot 'functions/development-secrets.ps1'
$connectionStringVariable = 'ConnectionStrings__PatientRegistry'
$telemetryConnectionStringVariable = 'ConnectionStrings__Telemetry'
$previousConnectionString = [Environment]::GetEnvironmentVariable(
    $connectionStringVariable,
    [EnvironmentVariableTarget]::Process)
$previousTelemetryConnectionString = [Environment]::GetEnvironmentVariable(
    $telemetryConnectionStringVariable,
    [EnvironmentVariableTarget]::Process)

. $secretFunctions

$effectiveConnectionString = $previousConnectionString
if ([string]::IsNullOrWhiteSpace($previousConnectionString)) {
    $postgresPassword = Get-EnvironmentFileValue -Path $environmentFile -Name 'POSTGRES_PASSWORD'
    if ([string]::IsNullOrWhiteSpace($postgresPassword)) {
        throw 'The PostgreSQL password is missing. Run eng/initialize-development.ps1 first.'
    }

    # The design-time factory reads this process-scoped value without exposing the password in command-line arguments.
    $effectiveConnectionString = "Host=localhost;Port=5432;Database=pulseguard;Username=pulseguard;Password=$postgresPassword"
    [Environment]::SetEnvironmentVariable(
        $connectionStringVariable,
        $effectiveConnectionString,
        [EnvironmentVariableTarget]::Process)
}

if ([string]::IsNullOrWhiteSpace($previousTelemetryConnectionString)) {
    [Environment]::SetEnvironmentVariable(
        $telemetryConnectionStringVariable,
        $effectiveConnectionString,
        [EnvironmentVariableTarget]::Process)
}

Push-Location $repositoryRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to restore the local .NET tools.'
    }

    & dotnet restore 'PulseGuard.slnx'
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to restore the PulseGuard solution before applying migrations.'
    }

    & dotnet ef database update `
        --project 'src/Services/PatientRegistry/PulseGuard.PatientRegistry.Infrastructure' `
        --startup-project 'src/Services/PatientRegistry/PulseGuard.PatientRegistry.Api' `
        --context PatientRegistryDbContext

    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to apply the Patient Registry migrations.'
    }

    & dotnet ef database update `
        --project 'src/Services/Telemetry/PulseGuard.Telemetry.Infrastructure' `
        --startup-project 'src/Services/Telemetry/PulseGuard.Telemetry.Grpc' `
        --context TelemetryDbContext

    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to apply the Telemetry migrations.'
    }
}
finally {
    [Environment]::SetEnvironmentVariable(
        $connectionStringVariable,
        $previousConnectionString,
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        $telemetryConnectionStringVariable,
        $previousTelemetryConnectionString,
        [EnvironmentVariableTarget]::Process)
    Pop-Location
}
