[CmdletBinding()]
param(
    [switch] $SkipRestore,
    [switch] $CollectCoverage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string] $FilePath,

        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
    }
}

Push-Location $repositoryRoot
try {
    if (-not $SkipRestore) {
        Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @('tool', 'restore')
        Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @('restore', 'PulseGuard.slnx')
    }

    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @(
        'format',
        'PulseGuard.slnx',
        '--verify-no-changes',
        '--no-restore'
    )

    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @(
        'build',
        'PulseGuard.slnx',
        '--configuration',
        'Release',
        '--no-restore'
    )

    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments @(
        'ef',
        'migrations',
        'has-pending-model-changes',
        '--no-build',
        '--configuration',
        'Release',
        '--project',
        'src/Services/PatientRegistry/PulseGuard.PatientRegistry.Infrastructure',
        '--startup-project',
        'src/Services/PatientRegistry/PulseGuard.PatientRegistry.Api',
        '--context',
        'PatientRegistryDbContext'
    )

    $testArguments = @(
        'test',
        'PulseGuard.slnx',
        '--configuration',
        'Release',
        '--no-build',
        '--results-directory',
        'artifacts/test-results'
    )

    if ($CollectCoverage) {
        $testArguments += '--collect:XPlat Code Coverage'
    }

    Invoke-CheckedCommand -FilePath 'dotnet' -Arguments $testArguments
}
finally {
    Pop-Location
}
