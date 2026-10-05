[CmdletBinding()]
param([switch]$Signed)
$ErrorActionPreference = 'Stop'
$flavor = if ($Signed) { 'signed' } else { 'unsigned' }
$source = Join-Path $PSScriptRoot "artifacts\$flavor\ComputerNameOverlay.exe"
if (-not (Test-Path -LiteralPath $source)) { throw "Missing binary: $source" }
$signature = Get-AuthenticodeSignature -LiteralPath $source
if ($Signed -and $signature.Status -ne 'Valid') { throw 'Signed packaging requires a valid Authenticode signature.' }
$metadata = [Diagnostics.FileVersionInfo]::GetVersionInfo($source)
if ($metadata.ProductName -ne 'Computer Name Overlay' -or $metadata.ProductVersion -ne '0.1.0') { throw 'Unexpected product metadata.' }
$version = $metadata.ProductVersion
$stage = Join-Path $PSScriptRoot ('artifacts\staging\' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $stage
foreach ($file in @('README.md','LICENSE','PRIVACY.md','CODE_SIGNING.md','MAINTAINERS.md','SECURITY.md','CONTRIBUTING.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs') -Destination $stage -Recurse
$release = Join-Path $PSScriptRoot 'artifacts\release'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$archive = Join-Path $release "ComputerNameOverlay-$version-windows-$flavor.zip"
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($archive + '.sha256'), ($hash + '  ' + [IO.Path]::GetFileName($archive) + "`n"), (New-Object Text.UTF8Encoding $false))
Write-Output "Packaged: $archive"
