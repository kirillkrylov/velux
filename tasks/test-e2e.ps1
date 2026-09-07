param([string]$EnvironmentName = 'Velux')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$settingsPath = Join-Path $env:LOCALAPPDATA 'creatio/clio/appsettings.json'
$registered = (Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json).Environments.$EnvironmentName
if (!$registered) { throw "Clio environment '$EnvironmentName' is not registered." }
$env:CREATIO_URL = $registered.Uri
$env:CREATIO_IS_NETCORE = 'true'
$env:CREATIO_USERNAME = $registered.Login
$env:CREATIO_PASSWORD = $registered.Password
$env:CREATIO_ACCESS_TOKEN = $null
$results = Join-Path $workspace ('artifacts/e2e/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $results | Out-Null
$allureResults = Join-Path $results 'allure-results'
$configPath = Join-Path $results 'allureConfig.json'
@{ allure = @{ directory = $allureResults; links = @() } } | ConvertTo-Json -Depth 4 | Set-Content $configPath
$env:ALLURE_CONFIG = $configPath
try {
    dotnet test "$workspace/tests/VibeCodingDemo.IntegrationTests/VibeCodingDemo.IntegrationTests.csproj" -c dev-n8 --logger "trx;LogFileName=e2e.trx" --results-directory $results
    if ($LASTEXITCODE -ne 0) { throw 'E2E tests failed. Review the TRX and Allure results.' }
} finally {
    if (Test-Path $allureResults) {
        allure generate $allureResults --config (Join-Path $workspace 'allurerc.mjs') --output (Join-Path $results 'allure-report')
        if ($LASTEXITCODE -ne 0) { throw 'Allure report generation failed.' }
    }
    $env:CREATIO_USERNAME = $null
    $env:CREATIO_PASSWORD = $null
    $env:ALLURE_CONFIG = $null
}
