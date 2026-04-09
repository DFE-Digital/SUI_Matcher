using Shared.Models;

namespace SUI.Client.Core.Application.Interfaces;

public interface IPersonSpecParser<TSource>
{
    List<PersonSpecification> Parse(List<TSource> records, HashSet<string> requiredHeaders);
}
