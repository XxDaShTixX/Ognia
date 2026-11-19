param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

$root       = Split-Path -Parent $MyInvocation.MyCommand.Path
$project    = Join-Path $root "..\Frontend\Frontend.csproj"
$publishDir = Join-Path $root "publish"
$outputDir  = Join-Path $root "Releases"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish $project -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

vpk pack --packId Ognia --packVersion $Version --packDir $publishDir --outputDir $outputDir --runtime win-x64 --channel win --icon "..\Frontend\Assets\Brand\Icon\Ognia.ico" --mainExe Frontend.exe

if ($LASTEXITCODE -ne 0) { throw "vpk pack failed." }

$setup = Join-Path $outputDir "Ognia-win-Setup.exe"
$renamed = Join-Path $outputDir "Ognia-v$Version.exe"
if (Test-Path $setup) { Move-Item $setup $renamed -Force }

Write-Host ""
Write-Host "Done. Output: $outputDir" -ForegroundColor Green