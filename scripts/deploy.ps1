param(
    [string]$Configuration = "Release",
    [string]$ArtifactsDir = "Artifacts"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root $ArtifactsDir
$addinArtifacts = Join-Path $artifacts "addin"

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
New-Item -ItemType Directory -Force -Path $addinArtifacts | Out-Null

dotnet restore (Join-Path $root "MSBuild/MemoryDb-Lib.sln")
dotnet pack (Join-Path $root "Memory-Db/Memory-Db.csproj") -c $Configuration -o $artifacts
dotnet build (Join-Path $root "XLS-Memory-Lib/XLS-Memory-Lib.csproj") -c $Configuration

Get-ChildItem -Path (Join-Path $root "XLS-Memory-Lib/bin/$Configuration") -Recurse -Include "*.xll", "*.dna" |
    Copy-Item -Destination $addinArtifacts -Force

Write-Host "NuGet package output: $artifacts"
Write-Host "Excel-DNA add-in output: $addinArtifacts"
