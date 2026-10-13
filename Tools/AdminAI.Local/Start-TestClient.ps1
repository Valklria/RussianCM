param([ValidateSet('Admin', 'Player')][string]$Role = 'Admin')
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$client = Join-Path $repoRoot 'bin/Content.Client/Content.Client.dll'
if (!(Test-Path -LiteralPath $client)) { throw 'Build the Windows client from this branch first.' }
$env:WSL_UTF8 = '1'
$null = & wsl.exe -d Ubuntu -- true
if ($LASTEXITCODE -ne 0) { throw 'Ubuntu is unavailable.' }
$address = '127.0.0.1' # WSL mirrored networking; the test server listens only on loopback.
$username = if ($Role -eq 'Admin') { 'AIAdminTest' } else { 'AIPlayerTest' }
Write-Output "Connecting $username to local Ubuntu at ${address}:1213."
if ($Role -eq 'Admin') {
    Write-Output 'In the game server console use listplayers, then promotehost with the actual name.'
    Write-Output 'Duplicate names receive a suffix, for example: promotehost AIAdminTest_2'
}
Push-Location $repoRoot
try {
    & dotnet $client --username $username --connect --connect-address "udp://${address}:1213"
    if ($LASTEXITCODE -ne 0) { throw "Client exited with code $LASTEXITCODE" }
} finally { Pop-Location }
