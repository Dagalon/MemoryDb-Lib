using DuckDb_Memory_Lib;

namespace DuckDB_Memory_Tests;

public class DuckToolsTests
{
    [SetUp]
    public void Setup()
    {
        ConnectionManager.GetInstance().CloseAllConnections();
    }

    [TearDown]
    public void TearDown()
    {
        ConnectionManager.GetInstance().CloseAllConnections();
    }

    [Test]
    public void T_QuoteIdentifier_And_ParameterName_Return_Safe_Sql_Tokens()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DuckTools.QuoteIdentifier("customer name"), Is.EqualTo("\"customer name\""));
            Assert.That(DuckTools.QuoteIdentifier("customer\"name"), Is.EqualTo("\"customer\"\"name\""));
            Assert.That(DuckTools.ParameterName(3), Is.EqualTo("@p3"));
            Assert.That(() => DuckTools.QuoteIdentifier(" "), Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void T_ResolveSql_Returns_Literal_Sql_Or_File_Contents()
    {
        const string sql = "SELECT 1 AS value";
        var sqlFilePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"ducktools_{Guid.NewGuid():N}.sql");
        File.WriteAllText(sqlFilePath, sql);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(DuckTools.ResolveSql(sql), Is.EqualTo(sql));
                Assert.That(DuckTools.ResolveSql(sqlFilePath), Is.EqualTo(sql));
                Assert.That(() => DuckTools.ResolveSql(" "), Throws.TypeOf<ArgumentException>());
            });
        }
        finally
        {
            if (File.Exists(sqlFilePath))
            {
                File.Delete(sqlFilePath);
            }
        }
    }

    [Test]
    public void T_GetListTables_And_DropTable_Handle_Existing_And_Missing_Tables()
    {
        var databaseId = $"TOOLS_DB_{Guid.NewGuid():N}";
        var tableId = $"TOOLS_TABLE_{Guid.NewGuid():N}";
        var conn = ConnectionManager.GetInstance().GetConnection(databaseId);

        var dbOutput = DuckTools.CreateDatabase(conn, databaseId, null);
        Assert.That(dbOutput.Output, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));

        var createOutput = QueryExecutor.CreateTable(conn, databaseId, tableId, ["id INTEGER"]);
        Assert.That(createOutput, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));

        var listTables = DuckTools.GetListTables(conn, databaseId);
        Assert.Multiple(() =>
        {
            Assert.That(listTables.Item1.Output, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
            Assert.That(listTables.Item2, Does.Contain(tableId));
        });

        var dropOutput = DuckTools.DropTable(conn, databaseId, tableId);
        Assert.Multiple(() =>
        {
            Assert.That(dropOutput.Item1.Output, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
            Assert.That(dropOutput.Item2, Is.EqualTo(string.Empty));
        });

        var missingDropOutput = DuckTools.DropTable(conn, databaseId, tableId);
        Assert.Multiple(() =>
        {
            Assert.That(missingDropOutput.Item1.Output, Is.EqualTo(EnumsDuckMemory.Output.TABLE_NOT_FOUND));
            Assert.That(missingDropOutput.Item2, Does.Contain(tableId));
        });
    }


    [Test]
    public void T_DeleteRegister_Ignores_Missing_Id()
    {
        Assert.DoesNotThrow(() => KeeperRegisterIdDataBase.DeleteRegister($"MISSING_{Guid.NewGuid():N}"));
    }

    [Test]
    public void T_CreateTable_Quotes_Database_And_Table_Identifiers()
    {
        var databaseId = $"TOOLS DB {Guid.NewGuid():N}";
        var tableId = $"TOOLS TABLE {Guid.NewGuid():N}";
        var conn = ConnectionManager.GetInstance().GetConnection(databaseId);

        var dbOutput = DuckTools.CreateDatabase(conn, databaseId, null);
        Assert.That(dbOutput.Output, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));

        var createOutput = QueryExecutor.CreateTable(conn, databaseId, tableId, ["id INTEGER"]);
        Assert.That(createOutput, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));

        var listTables = DuckTools.GetListTables(conn, databaseId);
        Assert.That(listTables.Item2, Does.Contain(tableId));
    }

    [Test]
    public void T_DeleteDataBase_Detaches_Existing_Database_And_Validates_Blank_Id()
    {
        var databaseId = $"DETACH_DB_{Guid.NewGuid():N}";
        var conn = ConnectionManager.GetInstance().GetConnection(databaseId);

        var dbOutput = DuckTools.CreateDatabase(conn, databaseId, null);
        Assert.That(dbOutput.Output, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
        Assert.That(DuckTools.GetListDataBase(conn), Does.Contain(databaseId));

        var detachOutput = DuckTools.DeleteDataBase(conn, databaseId);
        Assert.That(detachOutput.Output, Is.EqualTo(EnumsDuckMemory.Output.SUCCESS));
        Assert.That(DuckTools.GetListDataBase(conn), Does.Not.Contain(databaseId));

        var blankDetachOutput = DuckTools.DeleteDataBase(conn, string.Empty);
        Assert.That(blankDetachOutput.Output, Is.EqualTo(EnumsDuckMemory.Output.PATH_IS_NULL_OR_EMPTY));

        var failedDetachOutput = DuckTools.DeleteDataBase(conn, $"MISSING_{Guid.NewGuid():N}");
        Assert.Multiple(() =>
        {
            Assert.That(failedDetachOutput.Output, Is.EqualTo(EnumsDuckMemory.Output.DB_NOT_FOUND));
            Assert.That(failedDetachOutput.ExceptionMessage, Is.Not.Null.And.Not.Empty);
        });
    }
}
