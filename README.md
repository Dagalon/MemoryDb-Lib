# MemoryDb-Lib

A collection of helper libraries that make it simple to spin up disposable, in-memory database engines for local development, automated tests, and prototypes. The repository currently contains two .NET 8 class libraries:

- **LiteDb-Memory-Lib** – a façade over [LiteDB](https://www.litedb.org/) that keeps track of in-memory databases and exposes utility helpers for seeding data, executing ad-hoc queries, and working with LiteDB file storage.
- **SqliteDB-Memory-Lib** – a lightweight wrapper around the in-memory mode of Microsoft.Data.Sqlite with helpers to seed tables, execute SQL scripts, and map query results into strongly-typed objects.

Both libraries follow the same philosophy: offer an ergonomic API to create named in-memory databases, provide convenient seeding helpers, and make it trivial to clean up or persist data after a test run.

## Table of contents

- [Why use these libraries?](#why-use-these-libraries)
- [Project structure](#project-structure)
- [Requirements](#requirements)
- [Getting started](#getting-started)
  - [Build the solution](#build-the-solution)
  - [Reference the projects](#reference-the-projects)
- [LiteDb-Memory-Lib quickstart](#litedb-memory-lib-quickstart)
  - [Create and seed an in-memory database](#create-and-seed-an-in-memory-database)
  - [Load seed data from JSON](#load-seed-data-from-json)
  - [Work with LiteDB file storage](#work-with-litedb-file-storage)
  - [Run ad-hoc queries](#run-ad-hoc-queries)
  - [Persist a database to disk](#persist-a-database-to-disk)
- [SqliteDB-Memory-Lib quickstart](#sqlitedb-memory-lib-quickstart)
- [Testing](#testing)
- [License](#license)

## Why use these libraries?

Creating an in-memory database for a single test is straightforward, but making it repeatable, discoverable, and safe across an entire test suite is not. These libraries encapsulate the boilerplate so you can:

- Keep an inventory of named databases and share them across fixtures.
- Seed data from CLR objects or JSON payloads without manual mapping.
- Execute scripts or queries and deserialize the results into typed models.
- Persist databases to disk when you need to inspect state after a test.
- Integrate quickly with existing LiteDB or SQLite-based projects.

## Project structure

```
LiteDb-Memory-Lib/
├── LiteDb-Memory-Lib/           # LiteDB helpers and connection manager
├── LiteDb-Memory-Tests/         # Tests targeting LiteDb-Memory-Lib
├── SqliteDB-Memory-Lib/         # SQLite in-memory utilities
├── SqliteDb-Memory-Tests/       # Tests targeting SqliteDB-Memory-Lib
└── README.md
```

## Requirements

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [LiteDB](https://www.nuget.org/packages/LiteDB) (transitive dependency of LiteDb-Memory-Lib)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) (used for JSON seeding helpers)
- [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite) (used by SqliteDB-Memory-Lib)

## Getting started

### Build the solution

Clone the repository and run a build from the root directory:

```bash
dotnet build
```

### Reference the projects

Until packages are published to NuGet you can reference the projects directly from a consumer solution:

```bash
# LiteDB helper library
dotnet add <your-project> reference ../LiteDb-Memory-Lib/LiteDb-Memory-Lib/LiteDb-Memory-Lib.csproj

# SQLite helper library
dotnet add <your-project> reference ../LiteDb-Memory-Lib/SqliteDB-Memory-Lib/SqliteDB-Memory-Lib.csproj
```

### Generate a single DLL or NuGet package

If you prefer to consume both helper libraries as a single assembly, build the aggregated project located in `MemoryDb.Lib`.

```bash
# Produce MemoryDb.Lib.dll and a MemoryDb.Lib.<version>.nupkg file in ./artifacts
dotnet pack MemoryDb.Lib/MemoryDb.Lib.csproj -c Release
```

The resulting package exposes all the types that live in the two original projects, so you can reference a single DLL from test suites or publish the generated `.nupkg` to an internal feed. The pack target also emits a matching `.snupkg` file that contains symbols for debugging purposes.

### Download a prebuilt package

If you do not have the .NET SDK installed locally you can still grab the latest NuGet package from the CI workflow:

1. Navigate to the [**Pack MemoryDb.Lib** workflow](https://github.com/QTLando/MemoryDb-Lib/actions/workflows/pack-memorydb-lib.yml).
2. Open the most recent successful run on the `main` branch (or trigger a manual run via **Run workflow**).
3. Download the `memorydb-lib-nuget` artifact which includes both `.nupkg` and `.snupkg` files from the `artifacts/` folder.
4. Push the downloaded package to your preferred NuGet feed:

   ```bash
   dotnet nuget push MemoryDb.Lib.*.nupkg --api-key <token> --source <nuget-source>
   ```

Both artifacts contain the unified helpers from LiteDB and SQLite projects, so a single package reference lights up all available APIs.

## LiteDb-Memory-Lib quickstart

### Create and seed an in-memory database

```csharp
using LiteDb_Memory_Lib;
using System.Collections.Generic;

var manager = ConnectionManager.Instance();

manager.CreateDatabase("people-db");

var status = manager.CreateCollection("people-db", "people", new List<Person>
{
    new() { Id = 1, Name = "Ada" },
    new() { Id = 2, Name = "Grace" }
});

if (status == EnumsLiteDbMemory.Output.SUCCESS)
{
    var collection = manager.GetCollection<Person>("people-db", "people");
    var people = collection?.FindAll().ToList();
}
```

### Load seed data from JSON

```csharp
var seeded = manager.CreateCollection<Person>(
    alias: "people-db",
    collection: "people",
    path: "./data/people.json",
    useInsertBulk: true);
```

`Tools.ReadJson` throws descriptive exceptions when the file is missing or malformed, while `Tools.TryReadJson` returns a boolean so optional resources can be loaded without relying on exceptions for control flow.

### Work with LiteDB file storage

```csharp
var uploadResult = FileStorageTools.Upload(
    manager,
    alias: "people-db",
    id: "avatars",
    fileName: "ada.png",
    pathFile: "./assets/ada.png");

var fileInfo = FileStorageTools.Find(manager, "people-db", "avatars", "ada.png");
```

### Run ad-hoc queries

```csharp
var queryResults = GeneralTools.Execute<Person>(
    manager,
    "people-db",
    "SELECT * FROM people WHERE Name = 'Ada'"
);
```

### Persist a database to disk

```csharp
var result = manager.Close("people-db", pathToKeep: "./backups/people.db");
```

When `pathToKeep` is provided, the in-memory database is flushed to disk before the resources are disposed. This is helpful when you want to inspect data produced during a test run.

## SqliteDB-Memory-Lib quickstart

The SQLite-focused library mirrors the ergonomics of the LiteDB variant. A small example:

```csharp
using SqliteDB_Memory_Lib;
using System.Collections.Generic;

var manager = ConnectionManager.GetInstance();

// Obtain a shared in-memory connection identified by alias
var connection = manager.GetConnection("orders-db");

// Create a table and seed rows using the helper utilities
SqLiteLiteTools.CreateTable(
    connection,
    idDataBase: "main",
    idTable: "Orders",
    headers: new List<string> { "Id", "Customer", "Total" },
    values: new object[,]
    {
        { 1, "Ada", 120.5m },
        { 2, "Grace", 95.0m }
    });

// Read data back as a list of dictionaries
var orders = SqLiteLiteTools.Select(connection, "SELECT * FROM Orders");
```

The library exposes helpers to:

- Create or reuse in-memory SQLite connections by alias.
- Attach or create databases from disk paths.
- Build tables from CSV files, raw values, or object arrays.
- Map result sets into dictionaries or strongly-typed models via `QueryExecutor`.

Refer to the [SqliteDB-Memory-Lib](./SqliteDB-Memory-Lib) project for additional samples and extension points.

## Testing

Both libraries ship with dedicated test projects. Run the entire suite from the repository root:

```bash
dotnet test
```

## License

This project is licensed under the [MIT License](./LICENSE).
