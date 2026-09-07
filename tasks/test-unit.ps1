$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    dotnet test tests/VibeCodingDemo/VibeCodingDemo.Tests.csproj -c dev-n8 `
        /p:CollectCoverage=true /p:CoverletOutput=../../artifacts/unit/ /p:CoverletOutputFormat=cobertura `
        '/p:Include=[VibeCodingDemo]VibeCodingDemoApp.EmailDomains.*%2c[VibeCodingDemo]VibeCodingDemoApp.EntryPoints.EntityEventListeners.*' `
        /p:Threshold=80 '/p:ThresholdType=line%2cbranch' /p:ThresholdStat=total
    if ($LASTEXITCODE -ne 0) { throw 'Unit tests or the 80% line/branch coverage gate failed.' }
} finally { Pop-Location }
