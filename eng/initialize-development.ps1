[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$identityProject = Join-Path $repositoryRoot 'src/Identity/PulseGuard.Identity/PulseGuard.Identity.csproj'
$secretBytes = [byte[]]::new(32)

[System.Security.Cryptography.RandomNumberGenerator]::Fill($secretBytes)
$clientSecret = [Convert]::ToBase64String($secretBytes)

& dotnet user-secrets set 'Clients:Postman:ClientSecret' $clientSecret --project $identityProject
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to store the Postman client secret with .NET User Secrets.'
}

Write-Host 'Development configuration initialized.'
Write-Host 'Copy the following one-time value into the Postman clientSecret variable:'
Write-Host $clientSecret
