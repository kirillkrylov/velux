param(
    [Parameter(Mandatory)][string]$ClioTemplates,
    [Parameter(Mandatory)][string]$PackageLibraries
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
# Use licensed/local dependencies; these binaries must not be published in the public repository.
$testLibraries = Join-Path $ClioTemplates 'UnitTestLibs'
if (!(Test-Path (Join-Path $testLibraries 'UnitTest.dll'))) { throw 'ClioTemplates must contain UnitTestLibs/UnitTest.dll.' }
foreach ($name in @('ATF.Repository.dll', 'ErrorOr.dll')) {
    if (!(Test-Path (Join-Path $PackageLibraries $name))) { throw "PackageLibraries is missing $name." }
}
$testTarget = Join-Path $workspace 'tests/VibeCodingDemo/Libs'
$packageTarget = Join-Path $workspace 'packages/VibeCodingDemo/Files/Libs'
New-Item -ItemType Directory -Force $testTarget, $packageTarget | Out-Null
Copy-Item -Path (Join-Path $testLibraries '*.dll') -Destination $testTarget
foreach ($name in @('ATF.Repository.dll', 'ErrorOr.dll')) {
    Copy-Item -LiteralPath (Join-Path $PackageLibraries $name) -Destination $packageTarget
}
