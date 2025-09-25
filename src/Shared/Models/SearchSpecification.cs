namespace Shared.Models;

public class SearchSpecification : PersonSpecification
{
    public string SearchStrategy { get; set; } = SharedConstants.SearchStrategy.Strategies.Strategy1;
}