using SqliteDB_Memory_Lib;

namespace SqliteDb_Memory_Tests;

public class SqLiteToolsTests
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
    public void T_QuoteIdentifier_ParameterName_And_ResolveSql_Return_Safe_Sql_Tokens()
    {
        const string sql = "SELECT 1 AS value";
        var sqlFilePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"sqlitetools_{Guid.NewGuid():N}.sql");
        File.WriteAllText(sqlFilePath, sql);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(SqLiteLiteTools.QuoteIdentifier("customer name"), Is.EqualTo("\"customer name\""));
                Assert.That(SqLiteLiteTools.QuoteIdentifier("customer\"name"), Is.EqualTo("\"customer\"\"name\""));
                Assert.That(SqLiteLiteTools.ParameterName(4), Is.EqualTo("@p4"));
                Assert.That(SqLiteLiteTools.ResolveSql(sql), Is.EqualTo(sql));
                Assert.That(SqLiteLiteTools.ResolveSql(sqlFilePath), Is.EqualTo(sql));
                Assert.That(() => SqLiteLiteTools.QuoteIdentifier(" "), Throws.TypeOf<ArgumentException>());
                Assert.That(() => SqLiteLiteTools.ResolveSql(" "), Throws.TypeOf<ArgumentException>());
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
    public void T_GetListTables_DropTable_And_DeleteDataBase_Handle_Table_And_Database_Lifecycle()
    {
        var databaseId = $"SQLITE_DB_{Guid.NewGuid():N}";
        var tableId = $"SQLITE_TABLE_{Guid.NewGuid():N}";
        var manager = ConnectionManager.GetInstance();
        var conn = manager.GetConnection(databaseId);

        var dbOutput = SqLiteLiteTools.CreateDatabase(conn, databaseId, null);
        Assert.That(dbOutput, Is.EqualTo(EnumsSqliteMemory.Output.SUCCESS));

        var duplicateAttachOutput = SqLiteLiteTools.AttachedDataBase(conn, null, databaseId);
        Assert.That(duplicateAttachOutput, Is.EqualTo(EnumsSqliteMemory.Output.ERROR_TO_ATTACHED_DATABASE));

        var tableOutput = SqLiteLiteTools.CreateTable(
            conn,
            databaseId,
            tableId,
            ["Id", "Name"],
            new object[,] { { 1, "Alice" }, { 2, "Bob" } });
        Assert.That(tableOutput, Is.EqualTo(EnumsSqliteMemory.Output.SUCCESS));

        var listTables = SqLiteLiteTools.GetListTables(conn, databaseId);
        Assert.Multiple(() =>
        {
            Assert.That(listTables.Item1, Is.EqualTo(EnumsSqliteMemory.Output.SUCCESS));
            Assert.That(listTables.Item2, Does.Contain(tableId));
        });

        var dropOutput = SqLiteLiteTools.DropTable(conn, databaseId, tableId);
        Assert.Multiple(() =>
        {
            Assert.That(dropOutput.Item1, Is.EqualTo(EnumsSqliteMemory.Output.SUCCESS));
            Assert.That(dropOutput.Item2, Is.EqualTo(string.Empty));
        });

        var missingDropOutput = SqLiteLiteTools.DropTable(conn, databaseId, tableId);
        Assert.Multiple(() =>
        {
            Assert.That(missingDropOutput.Item1, Is.EqualTo(EnumsSqliteMemory.Output.TABLE_NOT_FOUND));
            Assert.That(missingDropOutput.Item2, Is.EqualTo($"Database '{databaseId}' does not contain table '{tableId}'."));
        });

        var deleteOutput = SqLiteLiteTools.DeleteDataBase(conn, databaseId);
        Assert.That(deleteOutput, Is.EqualTo(EnumsSqliteMemory.Output.SUCCESS));
        Assert.That(SqLiteLiteTools.GetListDataBase(conn), Does.Not.Contain(databaseId));
    }
}
