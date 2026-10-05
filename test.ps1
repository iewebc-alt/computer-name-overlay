[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'artifacts\unsigned\ComputerNameOverlay.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Run build.ps1 first.' }
$testDir = Join-Path $PSScriptRoot ('artifacts\tests\' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
$report = Join-Path $testDir 'checks.txt'
$process = Start-Process -FilePath $exe -ArgumentList @('--self-test', ('"' + $report + '"')) -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(30000)) {
    $process.Kill()
    throw 'Self-test timed out.'
}
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) {
    if (Test-Path -LiteralPath ($report + '.error.txt')) { Get-Content -LiteralPath ($report + '.error.txt') }
    throw 'Self-test failed.'
}
$result = Get-Content -LiteralPath $report -Raw
if (-not $result.StartsWith('PASS:')) { throw 'Unexpected self-test result.' }
Write-Output $result
