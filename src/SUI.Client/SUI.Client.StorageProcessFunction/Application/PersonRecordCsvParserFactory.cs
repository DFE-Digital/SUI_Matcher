using SUI.StorageProcessFunction.Application.Interfaces;
using SUI.StorageProcessFunction.Infrastructure.Csv;

namespace SUI.StorageProcessFunction.Application;

public class PersonRecordCsvParserFactory : IPersonRecordCsvParserFactory
{
    public IPersonSpecificationCsvParser Create(string parserToUse)
    {
        return parserToUse switch
        {
            "TypeOne" => new PersonSpecificationCsvParser(),
            _ => throw new InvalidOperationException("Unknown parser type."),
        };
    }
}
