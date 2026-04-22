using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Infrastructure.CsvParsers;

public sealed class CsvRequiredHeadersProvider(IOptions<CsvMatchDataOptions> csvMatchDataOptions)
    : ICsvRequiredHeadersProvider
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
