using CsvHelper.Configuration;
using SUI.Client.Core.Models;

namespace SUI.Client.Core;


public class DynamicCsvMap : ClassMap<MatchPersonPayload>
{
    public DynamicCsvMap(CsvMappingConfig mappingConfig)
    {
        var propertyMap = typeof(MatchPersonPayload).GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappingConfig.ColumnMappings)
        {
            if (propertyMap.TryGetValue(mapping.Value, out var propertyInfo))
            {
                Map(typeof(MatchPersonPayload), propertyInfo).Name(mapping.Key);
            }
        }
    }
}
