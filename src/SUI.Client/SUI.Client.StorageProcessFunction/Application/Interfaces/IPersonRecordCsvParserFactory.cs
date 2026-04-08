namespace SUI.StorageProcessFunction.Application.Interfaces;

public interface IPersonRecordCsvParserFactory
{
    IPersonSpecificationCsvParser Create(string parserToUse);
}
