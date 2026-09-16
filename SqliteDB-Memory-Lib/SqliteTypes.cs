using System.Data;
using System.Globalization;

namespace SqliteDB_Memory_Lib
{
    public static class NetTypeToSqLiteType
    {
        /// <summary>
        /// Translates a .NET <see cref="Type"/> into its SQLite <see cref="DbType"/> counterpart.
        /// </summary>
        public static string GetDbType(Type type)
        {
            return type switch
            {
                _ when type == typeof(string) => "TEXT",

                _ when type == typeof(int) => "INTEGER",
                _ when type == typeof(long) => "INTEGER",
                _ when type == typeof(bool) => "INTEGER",

                _ when type == typeof(double) => "REAL",
                _ when type == typeof(float) => "REAL",
                _ when type == typeof(decimal) => "REAL",

                _ when type == typeof(DateTime) => "TEXT",

                _ when type == typeof(byte[]) => "BLOB",

                _ => "TEXT"
            };
        }

        /// <summary>
        /// Attempts to infer the most appropriate .NET type for the provided string value.
        /// </summary>
        public static (object, Type) StrTryParse(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return (null, typeof(string))!;

            if (int.TryParse(value, CultureInfo.InvariantCulture, out int intValue))
                return (intValue, typeof(int));

            if (long.TryParse(value, CultureInfo.InvariantCulture, out long longValue))
                return (longValue, typeof(long));

            if (double.TryParse(value, CultureInfo.InvariantCulture, out double doubleValue))
                return (doubleValue, typeof(double));

            if (bool.TryParse(value, out bool boolValue))
                return (boolValue, typeof(bool));

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime dateValue))
                return (dateValue, typeof(DateTime));

            return (value, typeof(string));
        }

        /// <summary>
        /// Infers the .NET type of each column from the first row of data in the specified value range.
        /// </summary>
        public static List<Type> InferTypes(object[,] values, int noFields)
        {
            if (values.GetLength(0) == 0)
                throw new ArgumentException(
                    "Cannot infer column types from an empty values range.",
                    nameof(values));

            var noRows = values.GetLength(0);
            var types = new List<Type>(noFields);

            for (var j = 0; j < noFields; j++)
            {
                Type? detectedType = null;

                for (var i = 0; i < noRows; i++)
                {
                    var value = values[i, j];

                    // Ignore null values when inferring the column type
                    if (value == null || value == DBNull.Value)
                        continue;

                    var currentType = value switch
                    {
                        string => typeof(string),
                        int => typeof(int),
                        long => typeof(long),
                        double => typeof(double),
                        float => typeof(float),
                        decimal => typeof(decimal),
                        bool => typeof(bool),
                        DateTime => typeof(DateTime),
                        byte[] => typeof(byte[]),
                        _ => typeof(string)
                    };

                    // If any value is a string, treat the entire column as text
                    if (currentType == typeof(string))
                    {
                        detectedType = typeof(string);
                        break;
                    }

                    // Use the first non-null value as the initial detected type
                    detectedType ??= currentType;

                    // If the column contains mixed types, fall back to string
                    // to avoid losing information such as leading zeros
                    if (detectedType != currentType)
                    {
                        detectedType = typeof(string);
                        break;
                    }
                }

                // Default to string when the column contains only null values
                types.Add(detectedType ?? typeof(string));
            }

            return types;
        }
    }
}
