using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Infrastructure.CsvParsers;

public class PersonRecordCsvParserFactory : IPersonRecordCsvParserFactory
{
    public IPersonSpecificationCsvParser Create(string parserToUse)
    {
        return parserToUse switch
        {
            // Hard coded for now
            "TypeOne" =>
                new PersonSpecificationCsvParser(),
            _ => throw new InvalidOperationException("Unknown parser type."),
        };
    }
}
