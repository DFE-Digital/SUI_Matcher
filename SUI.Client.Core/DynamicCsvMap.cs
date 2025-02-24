using CsvHelper.Configuration;
using SUI.Client.Core.Models;

namespace SUI.Client.Core;


public class DynamicCsvMap : ClassMap<CsvRowModel>
{
    public DynamicCsvMap(CsvMappingConfig mappingConfig)
    {
        var propertyMap = typeof(CsvRowModel).GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappingConfig.ColumnMappings)
        {
            if (propertyMap.TryGetValue(mapping.Value, out var propertyInfo))
            {
                Map(typeof(CsvRowModel), propertyInfo).Name(mapping.Key);
            }
        }
    }
}
