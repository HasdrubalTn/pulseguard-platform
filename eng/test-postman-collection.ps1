[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$collectionPath = Join-Path $repositoryRoot 'postman/collections/PulseGuard.postman_collection.json'
$environmentPath = Join-Path $repositoryRoot 'postman/environments/PulseGuard.local.postman_environment.json'
$collectionContent = Get-Content -Path $collectionPath -Raw
$collection = $collectionContent | ConvertFrom-Json
$environment = Get-Content -Path $environmentPath -Raw | ConvertFrom-Json

$definedVariables = @($collection.variable | ForEach-Object { $_.key })
$definedVariables += @($environment.values | ForEach-Object { $_.key })
$referencedVariables = [regex]::Matches($collectionContent, '\{\{([A-Za-z][A-Za-z0-9]*)\}\}') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

$missingVariables = @($referencedVariables | Where-Object {
        $reference = $_
        -not ($definedVariables | Where-Object { $_ -ceq $reference })
    })

if ($missingVariables.Count -gt 0) {
    throw "Undefined Postman variables: $($missingVariables -join ', ')."
}

foreach ($baseUrlName in @('identityBaseUrl', 'patientApiBaseUrl')) {
    $baseUrl = $collection.variable | Where-Object { $_.key -ceq $baseUrlName } | Select-Object -First 1
    if ($null -eq $baseUrl -or [string]::IsNullOrWhiteSpace($baseUrl.value)) {
        throw "The collection variable $baseUrlName must define a local default URL."
    }
}

$clientSecret = $environment.values | Where-Object { $_.key -ceq 'clientSecret' } | Select-Object -First 1
if ($null -eq $clientSecret -or $clientSecret.type -cne 'secret') {
    throw 'The clientSecret environment variable must exist and use the secret type.'
}

Write-Host 'Postman collection variables are defined with case-sensitive names and safe local defaults.'
