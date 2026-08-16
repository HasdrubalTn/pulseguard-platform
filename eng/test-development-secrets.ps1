[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$secretFunctions = Join-Path $PSScriptRoot 'functions/development-secrets.ps1'

. $secretFunctions

$secret = New-DevelopmentSecret
$secretBytes = [Convert]::FromBase64String($secret)

if ($secretBytes.Length -ne 32) {
    throw "Expected a 32-byte development secret, but received $($secretBytes.Length) bytes."
}

$testFile = Join-Path ([System.IO.Path]::GetTempPath()) "pulseguard-$([Guid]::NewGuid()).env"

try {
    Set-Utf8NoBomContent -Path $testFile -Value @(
        'PULSEGUARD_TEST=value'
        'PULSEGUARD_SECRET=abc=def=='
    )
    $fileBytes = [System.IO.File]::ReadAllBytes($testFile)

    if ($fileBytes.Length -ge 3 -and
        $fileBytes[0] -eq 0xEF -and
        $fileBytes[1] -eq 0xBB -and
        $fileBytes[2] -eq 0xBF) {
        throw 'Expected UTF-8 content without a byte order mark.'
    }

    $environmentValue = Get-EnvironmentFileValue -Path $testFile -Name 'PULSEGUARD_SECRET'
    if ($environmentValue -ne 'abc=def==') {
        throw 'Expected the complete environment value, including equals signs.'
    }

    $missingValue = Get-EnvironmentFileValue -Path $testFile -Name 'PULSEGUARD_MISSING'
    if ($null -ne $missingValue) {
        throw 'Expected a missing environment value to return null.'
    }
}
finally {
    if (Test-Path $testFile) {
        Remove-Item $testFile -Force
    }
}

Write-Host 'Development secret generation is compatible with Windows PowerShell.'
