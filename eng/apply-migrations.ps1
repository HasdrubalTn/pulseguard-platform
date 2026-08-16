[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

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
    Pop-Location
}
