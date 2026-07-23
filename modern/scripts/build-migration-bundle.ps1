param(
    [string]$Runtime = "linux-x64",
    [string]$Output = "$PSScriptRoot/../artifacts/efbundle"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Resolve-Path "$PSScriptRoot/../.."
$outputPath = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force (Split-Path $outputPath) | Out-Null

Push-Location $repositoryRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed" }

    dotnet tool run dotnet-ef -- migrations bundle `
        --project modern/backend/src/BankOfZ.Infrastructure/BankOfZ.Infrastructure.csproj `
        --startup-project modern/backend/src/BankOfZ.Api/BankOfZ.Api.csproj `
        --configuration Release `
        --target-runtime $Runtime `
        --self-contained `
        --force `
        --output $outputPath
    if ($LASTEXITCODE -ne 0) { throw "migration bundle build failed" }
}
finally {
    Pop-Location
}

Write-Output $outputPath
