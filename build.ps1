[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Windows .NET Framework C# compiler is required.' }
$outputDir = Join-Path $PSScriptRoot 'artifacts\unsigned'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$exe = Join-Path $outputDir 'ComputerNameOverlay.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /warnaserror+ "/out:$exe" "/win32manifest:$PSScriptRoot\src\app.manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "$PSScriptRoot\src\ComputerNameOverlay.cs" "$PSScriptRoot\src\AssemblyInfo.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
$metadata = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if ($metadata.ProductName -ne 'Computer Name Overlay' -or $metadata.ProductVersion -ne '0.1.0') {
    throw 'Unexpected product metadata. Update the build/release version consistently.'
}
Write-Output "Built $exe ($($metadata.ProductVersion))"
