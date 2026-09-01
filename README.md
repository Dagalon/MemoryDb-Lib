# MemoryDb-Lib

A collection of helper libraries that make it simple to spin up disposable, in-memory database engines for local development, automated tests, prototypes, and Excel workbooks. The repository targets **.NET 10.0** and contains:

- **LiteDb-Memory-Lib** – a façade over [LiteDB](https://www.litedb.org/) that keeps track of in-memory databases and exposes utility helpers for seeding data, executing ad-hoc queries, and working with LiteDB file storage.
- **SqliteDB-Memory-Lib** – a lightweight wrapper around the in-memory mode of Microsoft.Data.Sqlite with helpers to seed tables, execute SQL scripts, and map query results into strongly-typed objects.
- **DuckDB-Memory-Lib** – helpers for in-memory DuckDB connections and SQL execution.
- **Memory-DB** – the NuGet packaging project that references the database helper libraries.
- **XLS-Memory-Lib** – an Excel-DNA add-in project that exposes Memory DB helpers as Excel worksheet functions and produces the `XLS-Memory-Lib.xll` add-in.

## Table of contents

- [Why use these libraries?](#why-use-these-libraries)
- [Project structure](#project-structure)
- [Class structure](#class-structure)
- [Requirements](#requirements)
- [Getting started](#getting-started)
  - [Build the solution](#build-the-solution)
  - [Reference the projects](#reference-the-projects)
  - [Deploy NuGet package and Excel add-in](#deploy-nuget-package-and-excel-add-in)
- [Excel add-in](#excel-add-in)
- [LiteDb-Memory-Lib quickstart](#litedb-memory-lib-quickstart)
- [SqliteDB-Memory-Lib quickstart](#sqlitedb-memory-lib-quickstart)
- [Testing](#testing)
- [License](#license)

## Why use these libraries?

Creating an in-memory database for a single test is straightforward, but making it repeatable, discoverable, and safe across an entire test suite is not. These libraries encapsulate the boilerplate so you can:

- Keep an inventory of named databases and share them across fixtures.
- Seed data from CLR objects, CSV files, SQL scripts, or JSON payloads without manual mapping.
- Execute scripts or queries and deserialize the results into typed models.
- Persist databases to disk when you need to inspect state after a test.
- Integrate quickly with existing LiteDB, SQLite, DuckDB, or Excel-based workflows.

## Project structure

```text
LiteDb-Memory-Lib/          # LiteDB helpers and connection manager
LiteDb-Memory-Tests/        # Tests targeting LiteDb-Memory-Lib
SqliteDB-Memory-Lib/        # SQLite in-memory utilities
SqliteDb-Memory-Tests/      # Tests targeting SqliteDB-Memory-Lib
DuckDB-Memory-Lib/          # DuckDB in-memory utilities
DuckDB-Memory-Tests/        # Tests targeting DuckDB-Memory-Lib
Memory-Db/                  # Memory.DB NuGet packaging project
XLS-Memory-Lib/             # Excel-DNA add-in project (XLS-Memory-Lib.xll)
  LiteDbExcelFunctions.cs  # LiteDB worksheet functions
  SqliteExcelFunctions.cs  # SQLite worksheet functions
  DuckDbExcelFunctions.cs  # DuckDB worksheet functions
scripts/deploy.sh           # Linux/macOS deployment helper
scripts/deploy.ps1          # PowerShell deployment helper
Artifacts/                  # Generated packages and add-ins
```

## Class structure

For class diagrams, package relationships, and Excel function alignment diagrams, see [CLASS_STRUCTURE.md](CLASS_STRUCTURE.md).

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Microsoft Excel for loading the generated Excel-DNA `.xll` add-in.
- [LiteDB](https://www.nuget.org/packages/LiteDB) (transitive dependency of LiteDb-Memory-Lib)
- [Microsoft.Data.Sqlite](https://www.nuget.org/packages/Microsoft.Data.Sqlite) (used by SqliteDB-Memory-Lib)
- [DuckDB.NET.Data.Full](https://www.nuget.org/packages/DuckDB.NET.Data.Full) (used by DuckDB-Memory-Lib)
- [ExcelDna.AddIn](https://www.nuget.org/packages/ExcelDna.AddIn) (used by XLS-Memory-Lib)

## Getting started

### Build the solution

Clone the repository and run a build from the root directory:

```bash
dotnet build MSBuild/MemoryDb-Lib.sln
```

### Reference the projects

Until packages are published to NuGet you can reference the projects directly from a consumer solution:

```bash
# Memory.DB aggregate package project
dotnet add <your-project> reference ./Memory-Db/Memory-Db.csproj

# Individual helper libraries
dotnet add <your-project> reference ./LiteDb-Memory-Lib/LiteDb-Memory-Lib.csproj
dotnet add <your-project> reference ./SqliteDB-Memory-Lib/SqliteDB-Memory-Lib.csproj
dotnet add <your-project> reference ./DuckDB-Memory-Lib/DuckDB-Memory-Lib.csproj
```

### Deploy NuGet package and Excel add-in

Use the deployment helper to generate both deliverables in one step:

```bash
./scripts/deploy.sh Release
```

On Windows or PowerShell:

```powershell
./scripts/deploy.ps1 -Configuration Release
```

The deploy command performs the following actions automatically:

1. Restores the solution.
2. Packs `Memory-Db/Memory-Db.csproj` into `Artifacts/Memory.DB.<version>.nupkg`.
3. Builds the Excel-DNA project `XLS-Memory-Lib/XLS-Memory-Lib.csproj`.
4. Copies the generated Excel add-in files (`.xll` and `.dna`) into `Artifacts/addin/`.

## Excel add-in

`XLS-Memory-Lib` is an Excel-DNA add-in compatible with the repository's .NET 10.0 projects. It produces a 64-bit add-in named `XLS-Memory-Lib.xll` and separates worksheet functions by database type using Excel categories: **Memory DB - LiteDB**, **Memory DB - SQLite**, and **Memory DB - DuckDB**.

Excel worksheet commands use a consistent text status contract: successful operations return `SUCCESS`, and failures return `ERROR: <message>`. Functions that spill tabular data return the same error format in the first cell when an operation fails.

### Common functions

| Function | Description |
| --- | --- |
| `MEMDB.VERSION()` | Returns the loaded add-in assembly version. |

### LiteDB functions (`Memory DB - LiteDB`)

| Function | Description |
| --- | --- |
| `MEMDB.LITEDB.CREATE(alias, [path], [replaceExisting], [shared])` | Creates/replaces an in-memory LiteDB database or opens a file-backed database. |
| `MEMDB.LITEDB.CLOSE(alias, [pathToKeep])` | Closes a LiteDB database and optionally saves it to disk. |
| `MEMDB.LITEDB.COLLECTIONS(alias)` | Spills the collection names for a LiteDB database. |
| `MEMDB.LITEDB.INSERT.JSON(alias, collection, jsonDocument)` | Inserts one JSON document into a LiteDB collection. |
| `MEMDB.LITEDB.FINDALL.JSON(alias, collection)` | Spills all documents in a LiteDB collection as JSON text. |
| `MEMDB.LITEDB.DELETE(alias, collection, id)` | Deletes one LiteDB document by id. |

### SQLite functions (`Memory DB - SQLite`)

| Function | Description |
| --- | --- |
| `MEMORY_DB.SQLITE.CREATE_DB(name, path)` | Opens or creates a named SQLite connection, optionally from a database file. |
| `MEMORY_DB.SQLITE.ATTACH(alias, databaseId, path, removeIfExist)` | Attaches an in-memory or file-backed SQLite database to a connection. |
| `MEMORY_DB.SQLITE.DATABASES(databaseId)` | Spills the attached SQLite database names. |
| `MEMORY_DB.SQLITE.TABLES(databaseId, dependency)` | Spills the tables for an attached SQLite database. |
| `MEMORY_DB.SQLITE.CREATE.TABLE(databaseId, table, range, dependency)` | Creates a SQLite table from an Excel range whose first row contains headers. |
| `MEMORY_DB.SQLITE.INSERT(databaseId, table, range, dependency)` | Inserts Excel range rows into a SQLite table. |
| `MEMORY_DB.SQLITE.EXECUTE(databaseId, sql, dependency)` | Executes a non-query SQLite statement. |
| `MEMORY_DB.SQLITE.SCALAR(databaseId, sql, dependency)` | Executes a scalar SQLite query. |
| `MEMORY_DB.SQLITE.QUERY(databaseId, sql, includeHeaders, parameters, dependency)` | Executes a parameterized SQLite query and spills a two-dimensional result. `parameters` is a two-column name/value range. |
| `MEMORY_DB.SQLITE.DROP.TABLE(databaseId, table)` | Drops a SQLite table. |
| `MEMORY_DB.SQLITE.SAVE(databaseId, path)` | Saves an attached SQLite database to a file. |
| `MEMORY_DB.SQLITE.CLOSE(databaseId)` | Closes a named SQLite connection. |
| `MEMORY_DB.SQLITE.CLOSE.ALL()` | Closes all SQLite connections. |

### DuckDB functions (`Memory DB - DuckDB`)

| Function | Description |
| --- | --- |
| `MEMORY_DB.DUCKDB.CREATE_DB(name, path)` | Opens or creates a named DuckDB connection, optionally from a database file. |
| `MEMORY_DB.DUCKDB.ATTACH(alias, databaseId, path, removeIfExist)` | Attaches an in-memory or file-backed DuckDB database to a connection. |
| `MEMORY_DB.DUCKDB.DATABASES(databaseId)` | Spills the attached DuckDB database names. |
| `MEMORY_DB.DUCKDB.TABLES(databaseId, dependency)` | Spills the tables for an attached DuckDB database. |
| `MEMORY_DB.DUCKDB.CREATE.TABLE(databaseId, table, range, dependency)` | Creates or replaces a DuckDB table from an Excel range whose first row contains headers. |
| `MEMORY_DB.DUCKDB.CREATE.PARQUET.TABLE(databaseId, table, parquetPath, dependency)` | Creates or replaces a DuckDB table from a parquet file path. |
| `MEMORY_DB.DUCKDB.INSERT(databaseId, table, range, dependency)` | Inserts Excel range rows into a DuckDB table. |
| `MEMORY_DB.DUCKDB.EXECUTE(databaseId, sql, dependency)` | Executes a non-query DuckDB statement. |
| `MEMORY_DB.DUCKDB.SCALAR(databaseId, sql, dependency)` | Executes a scalar DuckDB query. |
| `MEMORY_DB.DUCKDB.QUERY(databaseId, sql, includeHeaders, parameters, dependency)` | Executes a parameterized DuckDB query and spills a two-dimensional result. `parameters` is a two-column name/value range. |
| `MEMORY_DB.DUCKDB.DROP.TABLE(databaseId, table)` | Drops a DuckDB table. |
| `MEMORY_DB.DUCKDB.CLOSE(databaseId)` | Closes a named DuckDB connection. |
| `MEMORY_DB.DUCKDB.CLOSE.ALL()` | Closes all DuckDB connections. |

Example workbook formulas:

```excel
=MEMORY_DB.SQLITE.CREATE_DB("demo", "")
=MEMORY_DB.SQLITE.CREATE.TABLE("demo", "People", A1:B3, NOW())
=MEMORY_DB.SQLITE.QUERY("demo", "SELECT * FROM People WHERE Age >= $minAge", TRUE, {"minAge",18}, NOW())
=MEMORY_DB.DUCKDB.CREATE.PARQUET.TABLE("analytics", "Trips", "C:\\data\\trips.parquet", NOW())
```

Load `Artifacts/addin/XLS-Memory-Lib.xll` from Excel via **File > Options > Add-ins > Manage Excel Add-ins > Browse**.

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

Run the entire suite from the repository root:

```bash
dotnet test MSBuild/MemoryDb-Lib.sln
```

Build and package both deployment artifacts:

```bash
./scripts/deploy.sh Release
```

## License

This project is licensed under the [MIT License](./LICENSE).
