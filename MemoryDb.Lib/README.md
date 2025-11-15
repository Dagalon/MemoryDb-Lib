# MemoryDb.Lib NuGet package

`MemoryDb.Lib` bundles every helper type from the LiteDB and SQLite in-memory projects that live in this repository into a single
assembly. Install the package when you want the full toolset without juggling multiple project references.

## Install

```bash
# dotnet CLI
 dotnet add package MemoryDb.Lib --prerelease
```

The package targets **.NET 9.0** and exposes the following namespaces:

- `LiteDb_Memory_Lib.*`
- `SqliteDB_Memory_Lib.*`

## Features

- Unified dependency surface that brings both helper libraries into one DLL.
- Ships with symbols (`.snupkg`) so IDEs can provide full debugging support.
- Source files stay linked to their original folder structure, which makes stack traces easy to read.
- Packages created by the repository workflows are dropped into the `./artifacts` folder and uploaded as build artifacts so they
  can be downloaded without building locally.

## Build locally

```bash
dotnet pack MemoryDb.Lib/MemoryDb.Lib.csproj -c Release
ls artifacts
```

The command produces both `.nupkg` and `.snupkg` files inside the `artifacts` folder. Push them to NuGet feeds with `dotnet nuget
push` or the package manager of your choice.
