[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentFile = Join-Path $repositoryRoot '.env'
$secretFunctions = Join-Path $PSScriptRoot 'functions/development-secrets.ps1'
$connectionStringVariable = 'ConnectionStrings__PatientRegistry'
$previousConnectionString = [Environment]::GetEnvironmentVariable(
    $connectionStringVariable,
    [EnvironmentVariableTarget]::Process)

. $secretFunctions

if ([string]::IsNullOrWhiteSpace($previousConnectionString)) {
    $postgresPassword = Get-EnvironmentFileValue -Path $environmentFile -Name 'POSTGRES_PASSWORD'
    if ([string]::IsNullOrWhiteSpace($postgresPassword)) {
        throw 'The PostgreSQL password is missing. Run eng/initialize-development.ps1 first.'
    }

    # The design-time factory reads this process-scoped value without exposing the password in command-line arguments.
    $patientRegistryConnectionString = "Host=localhost;Port=5432;Database=pulseguard;Username=pulseguard;Password=$postgresPassword"
    [Environment]::SetEnvironmentVariable(
        $connectionStringVariable,
        $patientRegistryConnectionString,
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
}
finally {
    [Environment]::SetEnvironmentVariable(
        $connectionStringVariable,
        $previousConnectionString,
        [EnvironmentVariableTarget]::Process)
    Pop-Location
}
