using XLS_Memory_Lib;

namespace SqliteDB_Memory_Tests;

[TestFixture]
public class T_DuckDb_Excel_Query_Parameters
{
    [TestCase("value")]
    [TestCase("$value")]
    [TestCase("@value")]
    [TestCase(":value")]
    public void Query_Binds_Normalized_Parameter_Name(string key)
    {
        var databaseId = $"duckdb-parameters-{Guid.NewGuid():N}";
        try
        {
            var result = MemoryDbDuckDbExcelFunctions.Query(
                databaseId, "SELECT $value AS Result", true,
                new object[,] { { key, 42 } }, 0);

            Assert.That(result.GetLength(0), Is.EqualTo(2));
            Assert.That(result[0, 0], Is.EqualTo("Result"));
            Assert.That(Convert.ToInt64(result[1, 0]), Is.EqualTo(42));
        }
        finally
        {
            MemoryDbDuckDbExcelFunctions.Close(databaseId);
        }
    }

    [Test]
    public void Query_Preserves_Types_And_Converts_Empty_To_Null()
    {
        var databaseId = $"duckdb-parameter-types-{Guid.NewGuid():N}";
        try
        {
            var result = MemoryDbDuckDbExcelFunctions.Query(
                databaseId, "SELECT typeof($number), typeof($text), $empty IS NULL", false,
                new object[,]
                {
                    { "number", 12.5 },
                    { "text", "hello" },
                    { "empty", "" }
                }, 0);

            Assert.Multiple(() =>
            {
                Assert.That(result[0, 0], Is.EqualTo("DOUBLE"));
                Assert.That(result[0, 1], Is.EqualTo("VARCHAR"));
                Assert.That(result[0, 2], Is.EqualTo(true));
            });
        }
        finally
        {
            MemoryDbDuckDbExcelFunctions.Close(databaseId);
        }
    }
}
