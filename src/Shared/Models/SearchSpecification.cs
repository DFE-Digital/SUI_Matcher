namespace Shared.Models;

public class SearchSpecification : PersonSpecification
{
    public string SearchStrategy
    {
        get;
        init => field = string.IsNullOrEmpty(value) ? SharedConstants.SearchStrategy.Strategies.Strategy1 : value;
    } = SharedConstants.SearchStrategy.Strategies.Strategy1;
}