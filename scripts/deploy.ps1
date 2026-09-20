param(
    [string]$Configuration = "Release",
    [string]$ArtifactsDir = "Artifacts"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root $ArtifactsDir
$addinArtifacts = Join-Path $artifacts "addin"
$publishDir = Join-Path $root "XLS-Memory-Lib/bin/$Configuration/net10.0-windows/publish"
$requiredFiles = @("XLS-Memory-Lib.xll", "native/x64/e_sqlite3.dll", "native/x64/duckdb.dll")

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

dotnet restore (Join-Path $root "MSBuild/MemoryDb-Lib.sln")
if ($LASTEXITCODE -ne 0) { throw "Solution restore failed (exit $LASTEXITCODE)." }
dotnet pack (Join-Path $root "Memory-Db/Memory-Db.csproj") -c $Configuration -o $artifacts
if ($LASTEXITCODE -ne 0) { throw "NuGet packaging failed (exit $LASTEXITCODE)." }
dotnet build (Join-Path $root "XLS-Memory-Lib/XLS-Memory-Lib.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Excel add-in build failed (exit $LASTEXITCODE)." }

foreach ($relativePath in $requiredFiles) {
    $source = Join-Path $publishDir $relativePath
    if (!(Test-Path -LiteralPath $source -PathType Leaf) -or (Get-Item -LiteralPath $source).Length -eq 0) {
        throw "Required add-in publish file is missing or empty: $source"
    }
}

# Deploy the packed add-in and its complete directory layout, not loose build outputs.
New-Item -ItemType Directory -Force -Path $addinArtifacts | Out-Null
Get-ChildItem -LiteralPath $publishDir -Force |
    Copy-Item -Destination $addinArtifacts -Recurse -Force

foreach ($relativePath in $requiredFiles) {
    $sourceHash = (Get-FileHash -LiteralPath (Join-Path $publishDir $relativePath)).Hash
    $targetHash = (Get-FileHash -LiteralPath (Join-Path $addinArtifacts $relativePath)).Hash
    if ($sourceHash -ne $targetHash) { throw "Deployed file differs from publish output: $relativePath" }
}

Write-Host "NuGet package output: $artifacts"
Write-Host "Excel-DNA add-in output (including native DLLs): $addinArtifacts"
