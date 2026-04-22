using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Infrastructure.CsvParsers;

public sealed class CsvMatchingHeadersProvider(IOptions<CsvMatchDataOptions> csvMatchDataOptions)
    : ICsvHeadersProvider
{
    public IReadOnlyCollection<string> GetRequiredHeaders()
    {
        var columnMappings = csvMatchDataOptions.Value.ColumnMappings;

        return
        [
            columnMappings.Id,
            columnMappings.Given,
            columnMappings.Family,
            columnMappings.BirthDate,
            columnMappings.Postcode,
        ];
    }
}
