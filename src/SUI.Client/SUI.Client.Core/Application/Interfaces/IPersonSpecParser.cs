using Shared.Models;

namespace SUI.Client.Core.Application.Interfaces;

public interface IPersonSpecParser<TSource>
{
    PersonSpecification Parse(TSource record);
}
