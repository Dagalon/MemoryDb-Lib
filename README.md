# MemoryDb-Lib

MemoryDb-Lib provides named, in-memory database helpers for **LiteDB, SQLite and DuckDB**, plus a **64-bit Excel-DNA add-in**. It supports file-backed databases, data imports and synchronous coordination of operations on the same database alias.

The solution targets `net10.0-windows`. See [CLASS_STRUCTURE.md](CLASS_STRUCTURE.md) for the class inventory, relationships and exact worksheet signatures.

## Projects

| Project | Purpose |
| --- | --- |
| [LiteDb-Memory-Lib](LiteDb-Memory-Lib) | Document collections, JSON, filters, indexes and file storage. |
| [SqliteDB-Memory-Lib](SqliteDB-Memory-Lib) | SQLite connections, attached databases, SQL, CSV and table helpers. |
| [DuckDB-Memory-Lib](DuckDB-Memory-Lib) | DuckDB connections, queries, Parquet imports and connection configuration. |
| [Memory-Db](Memory-Db) | `Memory.DB` aggregate NuGet packaging project referencing all three libraries. |
| [XLS-Memory-Lib](XLS-Memory-Lib) | Worksheet functions and the `XLS-Memory-Lib.xll` add-in. |
| [Shared](Shared) | Source files linked into consuming projects; not a separate assembly. |
| LiteDb-Memory-Tests, SqliteDb-Memory-Tests, DuckDB-Memory-Tests | NUnit tests for engines, files, concurrency and selected Excel wrappers. |

## Requirements and dependencies

Use Windows with the .NET 10 SDK to build the full solution. Loading the add-in requires 64-bit Microsoft Excel and a compatible installed .NET runtime. C# 13 is the shared language setting; some test projects use `latest`.

The versions below are declared by the repository, not a claim that they are the latest available:

| Package | Version |
| --- | --- |
| LiteDB | 5.0.21 |
| Microsoft.Data.Sqlite | 10.0.12 |
| DuckDB.NET.Data.Full | 1.5.3 |
| CsvHelper | 33.1.0 |
| Newtonsoft.Json | 13.0.4 |
| ExcelDna.AddIn / ExcelDna.Integration | 1.10.0-preview4 |

The first five packages are declared in [Directory.Build.props](Directory.Build.props) and inherited by projects throughout the repository. Excel-DNA references are project-specific. SQLite is configured to a stable package; Excel-DNA remains a preview dependency. Keep shared dependency versions aligned rather than overriding SQLite independently in individual projects.

## Build and test

Run these commands from the repository root:

```powershell
dotnet restore MSBuild/MemoryDb-Lib.sln
dotnet build MSBuild/MemoryDb-Lib.sln -c Release --no-restore
dotnet test MSBuild/MemoryDb-Lib.sln -c Release --no-build
```

Replace `Release` with `Debug` for a Debug build. Tests exercise the managed Excel wrappers, not an interactive Excel session. No test pass count is asserted by this documentation refresh.

After changing dependency versions, regenerate the restore graph before building:

```powershell
dotnet restore MSBuild/MemoryDb-Lib.sln --force --force-evaluate
```

If an IDE continues reporting old dependency versions, reload the solution so imported properties are evaluated again.

To reference a library directly from a consumer project, use its project path, for example:

```powershell
dotnet add path/to/Consumer.csproj reference SqliteDB-Memory-Lib/SqliteDB-Memory-Lib.csproj
```

## C# examples

Each example is independent. Connection managers own their connections; close them through the manager. For raw handles, hold the alias operation scope throughout their use. Raw relational commands also lock the connection.

### SQLite

```csharp
using SqliteDB_Memory_Lib;

var manager = ConnectionManager.GetInstance();
try
{
    using var operation = manager.AcquireOperation("orders");
    var connection = manager.GetConnection("orders");
    var result = SqLiteLiteTools.CreateTable(connection, "main", "Orders",
        new List<string> { "Id", "Customer" },
        new object[,] { { 1, "Ada" }, { 2, "Grace" } });
    if (!result.IsSuccess)
        throw new InvalidOperationException(result.ExceptionMessage ?? result.ToString());

    var rows = SqLiteLiteTools.Select(connection, "SELECT * FROM main.Orders");
    Console.WriteLine(rows.Count);
}
finally
{
    manager.CloseConnection("orders");
}
```

`SqLiteLiteTools` supports CSV imports, insertion, SQL text or file resolution, attached database management, WAL activation, saving databases and CSV export. Relational query results are dictionaries of column names and values; there is no general typed-model mapper in these query executors.

### LiteDB

```csharp
using LiteDb_Memory_Lib;

var manager = ConnectionManager.Instance();
manager.CreateDatabase("people", substituteIfExist: true);
try
{
    manager.CreateCollection("people", "People", new List<Person>
    {
        new() { Id = 1, Name = "Ada" },
        new() { Id = 2, Name = "Grace" }
    });
    var rows = FilterTools.FindAll<Person>(manager, "people", "People");
    Console.WriteLine(rows?.Count ?? 0);
}
finally
{
    manager.Close("people");
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

Use `JsonTools.ReadJson<T>` / `TryReadJson<T>` for JSON files and `LiteDbTools.Execute<T>` for LiteDB SQL. `FileStorageTools.Upload` stores files; its `id` chooses the storage collection and `fileName` identifies the file. The stream overload reads `fileName` from disk when the stream is null. Protect use of handles returned by `GetCollection`, `GetDatabase` and `FileStorageTools.Find` with `AcquireOperation(alias)`.

`CreateCollection<T>` with null or empty document lists inserts a default `new T()` document. It is not an empty-collection operation. `Close(alias, pathToKeep)` saves an in-memory database before disposal; it does not copy an already file-backed database to a new location.

### DuckDB

```csharp
using DuckDb_Memory_Lib;

var manager = ConnectionManager.GetInstance();
try
{
    using var operation = manager.AcquireOperation("analytics");
    var connection = manager.GetConnection("analytics");
    var (status, rows) = QueryExecutor.ExecuteQryReader(connection,
        "SELECT $value AS answer", new Dictionary<string, string> { ["value"] = "42" });
    if (status != EnumsDuckMemory.Output.SUCCESS)
        throw new InvalidOperationException(status.ToString());
    Console.WriteLine(rows[0]["answer"]);
}
finally
{
    manager.CloseConnection("analytics");
}
```

`QueryExecutor.CreateParquetTable` creates or replaces a table using DuckDB's native Parquet reader. `DuckTools` provides attach/detach, table listing, scalar-function registration and SQL identifier helpers. Connection aliases and attached database names are distinct: an alias selects a managed connection; a catalog name selects a database inside that connection.

## Excel add-in

The current project builds only the 64-bit add-in. Build it with:

```powershell
dotnet build XLS-Memory-Lib/XLS-Memory-Lib.csproj -c Release
```

The packed output is under `XLS-Memory-Lib/bin/Release/net10.0-windows/publish/`. Preserve the deployment layout, including `native/x64` when present. Load the `.xll` through Excel's **File > Options > Add-ins > Manage Excel Add-ins > Browse**.

All registered function names start with `MEMORY_DB.`:

| Group | Functions |
| --- | --- |
| General | `VERSION`, `PATH`, `NATIVE.PATH` |
| `LITEDB` | `CREATE`, `CLOSE`, `COLLECTIONS`, `INSERT.JSON`, `FINDALL.JSON`, `DELETE` |
| `SQLITE` | `CREATE_DB`, `ATTACH`, `DATABASES`, `TABLES`, `CREATE.TABLE`, `INSERT`, `EXECUTE`, `SCALAR`, `QUERY`, `QUERY_TO_CSV`, `DROP.TABLE`, `SAVE`, `CLOSE`, `CLOSE.ALL` |
| `DUCKDB` | `CREATE_DB`, `ATTACH`, `DATABASES`, `TABLES`, `CREATE.TABLE`, `CREATE.PARQUET.TABLE`, `INSERT`, `EXECUTE`, `SCALAR`, `QUERY`, `DROP.TABLE`, `CLOSE`, `CLOSE.ALL` |

See the [worksheet signature reference](CLASS_STRUCTURE.md#worksheet-function-signatures) before constructing formulas. SQLite `CREATE.TABLE` has a `path` argument; SQLite `EXECUTE` accepts a parameter range, while DuckDB `EXECUTE` does not. Query parameter ranges contain name/value pairs without a header row. Prefer bound parameters for values.

Example: put `Id` and `Name` in A1:B1, then two data rows in A2:B3. Put `minId` and `1` in D1:E1. Use these formulas in separate cells:

```excel
G1: =MEMORY_DB.SQLITE.CREATE_DB("demo", "")
G2: =MEMORY_DB.SQLITE.CREATE.TABLE("demo", "People", A1:B3, "", G1)
G3: =MEMORY_DB.SQLITE.QUERY("demo", "SELECT * FROM demo.People WHERE Id >= $minId", TRUE, D1:E1, G2)
```

`G1:`, `G2:` and `G3:` identify destination cells and are not part of the formula. Regional Excel settings may require semicolons. The dependency cell references establish calculation order; the `dependency` values are otherwise unused by the implementation. Side-effecting functions can run again when Excel recalculates. Both Excel range-based SQLite and DuckDB table creation replace existing table contents.

Most command wrappers return `SUCCESS` or `ERROR: <message>`; tabular errors are returned in a single cell. `SQLITE.QUERY_TO_CSV` is an exception: it returns the raw operation status string. A query error can be represented as a table and then written to CSV, so an export status alone does not prove the query succeeded.

`CREATE_DB(name, path)` creates an in-memory root connection and attaches the requested database under `name`; a supplied file remains file-backed. Closing the alias releases the connection. The add-in's `AutoClose` currently performs no database cleanup; close managed databases explicitly when needed.

## DuckDB configuration

Place optional `memory-db.json` next to the `.xll`:

```json
{
  "temp_path": "duckdb-data/temp",
  "extension_path": "duckdb-data/extensions"
}
```

Missing, null or blank values use the add-in directory. Relative paths resolve against that directory. Outside Excel, the initial base is `AppContext.BaseDirectory`; call `DuckDbConfiguration.Initialize(baseDirectory, configurationFile)` before opening connections to override it.

The library creates required directories and applies `extension_directory` and a distinct `temp_directory` under `duckdb-temp/<connection-id>` for each new connection. Existing connections retain their settings; close and reopen them to apply changes. Invalid JSON or inaccessible directories cause errors. Empty temporary directories may remain after closing connections.

## Concurrency and local files

Operations using the same alias are serialized; different aliases can progress independently. This coordination is synchronous: dispose `AcquireOperation` on the acquiring thread, never hold it across `await`, and avoid acquiring other aliases inside it. Direct relational commands should also use `lock (connection)` within the alias scope. Do not retain handles past close or replacement. Relational `CloseAllConnections` closes a snapshot; aliases opened afterward are not included.

CSV, JSON, SQL readers and LiteDB uploads open input files with `FileShare.ReadWrite | FileShare.Delete`. Another application must also permit reading. Exclusive locks still fail, and a concurrently modified file is not a consistent snapshot. Parquet uses DuckDB's own reader.

SQLite and DuckDB connection factories open or create the supplied database path; without a path they use memory. LiteDB's `CreateDatabase` requires a supplied path to already exist, and file sharing must be requested with `isShared: true` (Excel: `shared=true`). Native database locking rules still apply. Different aliases pointing at the same file do not bypass those rules. SQLite WAL reads can observe committed data while another connection writes.

## Packaging and CI

```powershell
# Aggregate NuGet package
dotnet pack Memory-Db/Memory-Db.csproj -c Release -o Artifacts

# Restore, pack, build and deploy the complete packed add-in with native DLLs
./scripts/deploy.ps1 -Configuration Release -ArtifactsDir Artifacts
```

The script produces `Artifacts/Memory.DB.<version>.nupkg` and copies the complete packed `publish` directory into `Artifacts/addin`, preserving subdirectories. It requires a nonempty `XLS-Memory-Lib.xll`, `native/x64/e_sqlite3.dll` and `native/x64/duckdb.dll`, and verifies that deployed copies match the publish output. Build failures stop deployment. Any optional `memory-db.json` already present in `publish` is copied too; otherwise place it beside the deployed `.xll`. Distribute the entire `Artifacts/addin` directory, not only the `.xll`. `scripts/deploy.sh` implements the analogous Bash sequence; its presence does not establish support for building the Windows Excel add-in on Linux or macOS.

`Memory.DB` references the three engine projects. Normal packing records those project dependencies; it is not a single package bundling all engine assemblies. A consumer feed must provide the referenced packages as well.

[CI](.github/workflows/ci.yml) builds and tests pull requests and pushes targeting `develop` / `main` on `windows-latest`. After successful tests on `main`, separate jobs create NuGet and Excel add-in artifacts. They upload workflow artifacts; they do not publish to NuGet.org or install the add-in on a user's machine. The workflow currently uses `checkout@v7`, `setup-dotnet@v6` and `upload-artifact@v7` with the .NET 10 SDK.

## License

[MIT](LICENSE).
