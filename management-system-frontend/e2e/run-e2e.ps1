$ErrorActionPreference = 'Stop'

$postgresRoot = 'C:\Program Files\PostgreSQL'
$postgresBin = Get-ChildItem -Path $postgresRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'bin' } |
    Where-Object { Test-Path (Join-Path $_ 'psql.exe') } |
    Select-Object -First 1

if (-not $postgresBin) {
    throw 'PostgreSQL command-line tools were not found under C:\Program Files\PostgreSQL.'
}

$pgHost = Read-Host 'PostgreSQL host [localhost]'
if ([string]::IsNullOrWhiteSpace($pgHost)) { $pgHost = 'localhost' }
$pgPort = Read-Host 'PostgreSQL port [5432]'
if ([string]::IsNullOrWhiteSpace($pgPort)) { $pgPort = '5432' }
$pgUser = Read-Host 'PostgreSQL user [postgres]'
if ([string]::IsNullOrWhiteSpace($pgUser)) { $pgUser = 'postgres' }
$pgSecure = Read-Host 'PostgreSQL password (input is hidden)' -AsSecureString
$pgPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pgSecure)
$pgPassword = $null
$databaseCreated = $false
$testExitCode = 1
$databaseName = "task_management_e2e_$([Guid]::NewGuid().ToString('N').Substring(0, 10))"
$env:E2E_DATABASE_NAME = $databaseName
$env:E2E_ADMIN_EMAIL = "e2e-admin-$([Guid]::NewGuid().ToString('N').Substring(0, 8))@example.test"

try {
    $pgPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pgPointer)
    $env:PGPASSWORD = $pgPassword

    $connectionBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()
    [void]$connectionBuilder.Add('Host', [string]$pgHost)
    [void]$connectionBuilder.Add('Port', [string]$pgPort)
    [void]$connectionBuilder.Add('Database', [string]$databaseName)
    [void]$connectionBuilder.Add('Username', [string]$pgUser)
    [void]$connectionBuilder.Add('Password', [string]$pgPassword)
    $env:E2E_DATABASE_CONNECTION = $connectionBuilder.ConnectionString

    $adminBytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($adminBytes)
        $env:E2E_ADMIN_PASSWORD = [Convert]::ToBase64String($adminBytes)
    }
    finally {
        $rng.Dispose()
        [Array]::Clear($adminBytes, 0, $adminBytes.Length)
        Remove-Variable adminBytes, rng -ErrorAction SilentlyContinue
    }

    & (Join-Path $postgresBin 'createdb.exe') -h $pgHost -p $pgPort -U $pgUser -O $pgUser $databaseName
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not create the isolated end-to-end database. Check PostgreSQL is running and the supplied credentials are valid.'
    }
    $databaseCreated = $true

    Push-Location (Split-Path -Parent $PSScriptRoot)
    try {
        npm run e2e
        $testExitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
}
finally {
    if ($databaseCreated) {
        & (Join-Path $postgresBin 'dropdb.exe') -h $pgHost -p $pgPort -U $pgUser $databaseName
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not remove the temporary database '$databaseName'. Remove it manually after confirming it contains only end-to-end test data."
            if ($testExitCode -eq 0) { $testExitCode = 1 }
        }
    }

    Remove-Item Env:PGPASSWORD, Env:E2E_DATABASE_NAME, Env:E2E_DATABASE_CONNECTION, Env:E2E_ADMIN_EMAIL, Env:E2E_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pgPointer)
    Remove-Variable pgPointer, pgSecure, pgPassword, connectionBuilder -ErrorAction SilentlyContinue
}

exit $testExitCode
