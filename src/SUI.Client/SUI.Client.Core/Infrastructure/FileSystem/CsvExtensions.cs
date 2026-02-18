using System.Data;

namespace SUI.Client.Core.Infrastructure.FileSystem;

public static class CsvExtensions
{
    public static string GetFirstValueOrDefault(this DataRow row, IEnumerable<string> possibleColumnNames)
    {
        foreach (var columnName in possibleColumnNames)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                continue;
            }

            var columnValue = row.Field<string>(columnName);
            if (!string.IsNullOrEmpty(columnValue))
            {
                return columnValue;
            }
        }
        return string.Empty;
    }
}