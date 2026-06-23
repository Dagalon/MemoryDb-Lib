using System.Globalization;
using ExcelDna.Integration;

namespace XLS_Memory_Lib;

internal static class Tables
{
    public static object[,] Vector(string header, IEnumerable<object> values)
    {
        var rows = values.ToList();
        var result = new object[rows.Count + 1, 1];
        result[0, 0] = header;
        for (var i = 0; i < rows.Count; i++)
        {
            result[i + 1, 0] = NormalizeCell(rows[i]);
        }

        return result;
    }

    public static object[,] ReaderToArray(System.Data.IDataReader reader, bool includeHeaders)
    {
        var fieldCount = reader.FieldCount;
        var rows = new List<object[]>();

        if (includeHeaders)
        {
            rows.Add(Enumerable.Range(0, fieldCount).Select(reader.GetName).Cast<object>().ToArray());
        }

        while (reader.Read())
        {
            var values = new object[fieldCount];
            reader.GetValues(values);
            rows.Add(values.Select(NormalizeCell).ToArray());
        }

        if (rows.Count == 0)
        {
            return new object[,] { { ExcelEmpty.Value } };
        }

        var result = new object[rows.Count, fieldCount];
        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < fieldCount; column++)
            {
                result[row, column] = rows[row][column];
            }
        }

        return result;
    }

    public static bool TryRangeToHeadersAndValues(object[,] range, out List<string> headers, out object[,] values, out string error)
    {
        headers = [];
        values = new object[0, 0];
        error = string.Empty;

        if (range.GetLength(0) < 2 || range.GetLength(1) < 1)
        {
            error = "range must include a header row and at least one data row";
            return false;
        }

        var columns = range.GetLength(1);
        for (var column = 0; column < columns; column++)
        {
            var header = Convert.ToString(range[0, column], CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(header))
            {
                error = $"header at column {column + 1} is empty";
                return false;
            }

            headers.Add(header.Trim());
        }

        var rows = range.GetLength(0) - 1;
        values = new object[rows, columns];
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                values[row, column] = NormalizeInput(range[row + 1, column]);
            }
        }

        return true;
    }

    public static object[,] ErrorTable(string message) => new object[,] { { $"ERROR: {message}" } };

    public static object NormalizeInput(object value)
    {
        return value switch
        {
            null => DBNull.Value,
            ExcelEmpty => DBNull.Value,
            ExcelMissing => DBNull.Value,
            ExcelError => DBNull.Value,
            string text when string.IsNullOrEmpty(text) => DBNull.Value,
            _ => value
        };
    }

    private static object NormalizeCell(object value)
    {
        return value == DBNull.Value || value is null ? ExcelEmpty.Value : value;
    }
}
