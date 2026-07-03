# MemoryDb-Lib Class Structure

This document maps the main projects, classes, and runtime relationships in **MemoryDb-Lib**.

## High-level package map

```mermaid
flowchart LR
    Consumer[Consumer app or tests] --> MemoryDb[Memory-DB package]
    Consumer --> Excel[XLS-Memory-Lib Excel-DNA add-in]

    MemoryDb --> Lite[LiteDb-Memory-Lib]
    MemoryDb --> SQLite[SqliteDB-Memory-Lib]
    MemoryDb --> Duck[DuckDB-Memory-Lib]

    Excel --> Lite
    Excel --> SQLite
    Excel --> Duck

    Lite --> LiteDB[(LiteDB)]
    SQLite --> SqliteEngine[(Microsoft.Data.Sqlite)]
    Duck --> DuckEngine[(DuckDB.NET)]
```

## Core database helper classes

```mermaid
classDiagram
    direction LR

    namespace LiteDb_Memory_Lib {
        class ConnectionManager
        class LiteDbTools
        class FilterTools
        class FileStorageTools
        class JsonTools
        class EnumsLiteDbMemory
    }

    namespace SqliteDB_Memory_Lib {
        class ConnectionManager
        class KeeperRegisterIdDataBase
        class SqLiteLiteTools
        class QueryExecutor
        class NetTypeToSqLiteType
        class Extensions
        class EnumsSqliteMemory
    }

    namespace DuckDb_Memory_Lib {
        class ConnectionManager
        class KeeperRegisterIdDataBase
        class DuckTools
        class QueryExecutor
        class NetTypeToDuckDbType
        class EntityAppenderMap~T~
        class EnumsDuckMemory
    }

    LiteDbTools --> ConnectionManager : opens/reuses aliases
    FilterTools --> ConnectionManager : query helpers
    FileStorageTools --> ConnectionManager : file storage helpers
    JsonTools --> LiteDbTools : JSON seed/query support

    SqLiteLiteTools --> ConnectionManager : opens/reuses aliases
    SqLiteLiteTools --> QueryExecutor : create/insert/query
    QueryExecutor --> NetTypeToSqLiteType : type inference
    QueryExecutor --> Extensions : object mapping
    ConnectionManager *-- KeeperRegisterIdDataBase : path registry

    DuckTools --> ConnectionManager : opens/reuses aliases
    DuckTools --> QueryExecutor : query helpers
    QueryExecutor --> NetTypeToDuckDbType : type mapping
    EntityAppenderMap~T~ --> DuckTools : appender mapping support
    ConnectionManager *-- KeeperRegisterIdDataBase : path registry
```

## Excel add-in surface

SQLite and DuckDB worksheet functions intentionally expose matching headers for day-to-day relational work:

```mermaid
flowchart TB
    subgraph SQLite[Memory DB - SQLite]
        SCreateDb[CREATE_DB name, path]
        SAttach[ATTACH alias, databaseId, path, removeIfExist]
        SDatabases[DATABASES databaseId]
        STables[TABLES databaseId, dependency]
        SCreateTable[CREATE.TABLE databaseId, table, range, dependency]
        SInsert[INSERT databaseId, table, range, dependency]
        SExecute[EXECUTE databaseId, sql, dependency]
        SScalar[SCALAR databaseId, sql, dependency]
        SQuery[QUERY databaseId, sql, includeHeaders, dependency]
        SDrop[DROP.TABLE databaseId, table]
        SClose[CLOSE databaseId]
        SCloseAll[CLOSE.ALL]
    end

    subgraph DuckDB[Memory DB - DuckDB]
        DCreateDb[CREATE_DB name, path]
        DAttach[ATTACH alias, databaseId, path, removeIfExist]
        DDatabases[DATABASES databaseId]
        DTables[TABLES databaseId, dependency]
        DCreateTable[CREATE.TABLE databaseId, table, range, dependency]
        DInsert[INSERT databaseId, table, range, dependency]
        DExecute[EXECUTE databaseId, sql, dependency]
        DScalar[SCALAR databaseId, sql, dependency]
        DQuery[QUERY databaseId, sql, includeHeaders, dependency]
        DDrop[DROP.TABLE databaseId, table]
        DClose[CLOSE databaseId]
        DCloseAll[CLOSE.ALL]
    end

    SCreateDb -. aligned .- DCreateDb
    SAttach -. aligned .- DAttach
    SDatabases -. aligned .- DDatabases
    STables -. aligned .- DTables
    SCreateTable -. aligned .- DCreateTable
    SInsert -. aligned .- DInsert
    SExecute -. aligned .- DExecute
    SScalar -. aligned .- DScalar
    SQuery -. aligned .- DQuery
    SDrop -. aligned .- DDrop
    SClose -. aligned .- DClose
    SCloseAll -. aligned .- DCloseAll
```

## Typical execution flow

```mermaid
sequenceDiagram
    participant Excel as Excel formula
    participant AddIn as XLS-Memory-Lib
    participant Manager as ConnectionManager
    participant Tools as SqLiteLiteTools / DuckTools
    participant Engine as SQLite / DuckDB engine

    Excel->>AddIn: CREATE_DB(name, path)
    AddIn->>Manager: GetConnection(name, path)
    Manager->>Tools: GetInstance(path)
    Tools-->>Manager: open connection
    AddIn->>Tools: CreateDatabase(connection, name, path)
    Tools->>Engine: ATTACH database
    Tools-->>AddIn: SUCCESS / error output

    Excel->>AddIn: QUERY(databaseId, sql, includeHeaders, dependency)
    AddIn->>Tools: ResolveSql(sql)
    AddIn->>Engine: execute query
    Engine-->>AddIn: data reader
    AddIn-->>Excel: two-dimensional spill range
```
