using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Util;
using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Infrastructure.FileSystem;

namespace SUI.Client.Core.Infrastructure.CsvParsers;

public class CsvPersonSpecParser(IOptions<CsvMatchDataOptions> csvMatchDataOptions)
    : IPersonSpecParser<CsvRecordDto>
{
    public PersonSpecification Parse(CsvRecordDto csvRecord)
    {
        string[] acceptedDateFormats = [csvMatchDataOptions.Value.DateFormat];
        var record = csvRecord.Record;

        var dob = (
            record.GetFirstValueOrDefault([csvMatchDataOptions.Value.ColumnMappings.BirthDate])
        ).Trim();
        var birthDate = dob.ToDateOnly(acceptedDateFormats);
        return new PersonSpecification
        {
            Given = record.GetFirstValueOrDefault([csvMatchDataOptions.Value.ColumnMappings.Given]),
            Family = record.GetFirstValueOrDefault([
                csvMatchDataOptions.Value.ColumnMappings.Family,
            ]),
            BirthDate = birthDate,
            Email = record.GetFirstValueOrDefault([csvMatchDataOptions.Value.ColumnMappings.Email]),
            AddressPostalCode = record.GetFirstValueOrDefault([
                csvMatchDataOptions.Value.ColumnMappings.Postcode,
            ]),
            Gender = record.GetFirstValueOrDefault([
                csvMatchDataOptions.Value.ColumnMappings.Gender,
            ]),
            Phone = record.GetFirstValueOrDefault([csvMatchDataOptions.Value.ColumnMappings.Phone]),
            RawBirthDate = [dob],
        };
    }
}
