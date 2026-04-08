namespace SUI.Client.Core.Application.Interfaces;

public interface IPersonRecordCsvParserFactory
{
    IPersonSpecificationCsvParser Create(string parserToUse);
}
