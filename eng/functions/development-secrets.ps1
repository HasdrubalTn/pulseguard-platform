function New-DevelopmentSecret {
    [CmdletBinding()]
    param(
        [ValidateRange(16, 1024)]
        [int] $ByteCount = 32
    )

    $secretBytes = New-Object byte[] $ByteCount
    $randomNumberGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()

    try {
        $randomNumberGenerator.GetBytes($secretBytes)
        return [Convert]::ToBase64String($secretBytes)
    }
    finally {
        $randomNumberGenerator.Dispose()
    }
}

function Set-Utf8NoBomContent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string[]] $Value
    )

    $utf8WithoutBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllLines($Path, $Value, $utf8WithoutBom)
}
