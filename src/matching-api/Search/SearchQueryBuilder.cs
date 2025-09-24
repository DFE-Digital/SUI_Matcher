using Shared;
using Shared.Models;

namespace MatchingApi.Search;

public class SearchQueryBuilder
{
    private readonly OrderedDictionary<string, SearchQuery> _queries = new();
    private readonly SearchSpecification _model;
    private readonly int _dobRange;

    public SearchQueryBuilder(SearchSpecification model, int dobRange = 6)
    {
        if (!model.BirthDate.HasValue)
        {
            throw new InvalidOperationException("Birthdate is required for search queries");
        }

        _model = model;
        _dobRange = dobRange;
    }
    
    private string[]? ModelName => _model.Given is not null ? [_model.Given] : null;
    private string[] DobRange =>
    [
        "ge" + _model.BirthDate!.Value.AddMonths(-_dobRange).ToString(SharedConstants.SearchQuery.DateFormat),
        "le" + _model.BirthDate.Value.AddMonths(_dobRange).ToString(SharedConstants.SearchQuery.DateFormat)
    ];
    private string[] Dob => ["eq" + _model.BirthDate!.Value.ToString(SharedConstants.SearchQuery.DateFormat)];

    public void AddNonFuzzyGfd()
    {
        _queries.Add("NonFuzzyGFD", new SearchQuery()
        {
            ExactMatch = false, Given = ModelName, Family = _model.Family, Birthdate = Dob
        });
    }
    
    public void AddNonFuzzyGfdRange()
    {
        _queries.Add("NonFuzzyGFDRange", new SearchQuery()
        {
            ExactMatch = false, Given = ModelName, Family = _model.Family, Birthdate = DobRange
        });
    }

    public void AddNonFuzzyAll()
    {
        _queries.Add("NonFuzzyAll", new SearchQuery()
        {
            ExactMatch = false,
            Given = ModelName,
            Family = _model.Family,
            Email = _model.Email,
            Gender = _model.Gender,
            Phone = _model.Phone,
            Birthdate = Dob,
            AddressPostalcode = _model.AddressPostalCode,
        });
    }
    
    public void AddFuzzyGfd()
    {
        _queries.Add("FuzzyGFD", new SearchQuery()
        {
            FuzzyMatch = true, Given = ModelName, Family = _model.Family, Birthdate = Dob
        });
    }

    public void AddFuzzyAll()
    {
        _queries.Add("FuzzyAll", new SearchQuery()
        {
            FuzzyMatch = true,
            Given = ModelName,
            Family = _model.Family,
            Email = _model.Email,
            Gender = _model.Gender,
            Phone = _model.Phone,
            Birthdate = Dob,
            AddressPostalcode = _model.AddressPostalCode
        });
    }
    
    public void AddFuzzyGfdRangePostcode()
    {
        _queries.Add("FuzzyGFDRangePostcode",
            new SearchQuery()
            {
                FuzzyMatch = true, 
                Given = ModelName, 
                Family = _model.Family, 
                Birthdate = DobRange, 
                AddressPostalcode = _model?.AddressPostalCode
            });
    }

    public void AddFuzzyGfdRange()
    {
        _queries.Add("FuzzyGFDRange",
            new SearchQuery()
            {
                FuzzyMatch = true, 
                Given = ModelName, 
                Family = _model.Family, 
                Birthdate = DobRange
            });
    }
    
    public void AddExactGfd()
    {
        _queries.Add("ExactGFD", new SearchQuery()
        {
            ExactMatch = true,
            Given = ModelName,
            Family = _model.Family,
            Birthdate = Dob
        });
    }

    public void AddExactAll()
    {
        _queries.Add("ExactAll", new SearchQuery()
        {
            ExactMatch = true,
            Given = ModelName,
            Family = _model.Family,
            Email = _model.Email,
            Gender = _model.Gender,
            Phone = _model.Phone,
            Birthdate = Dob,
            AddressPostalcode = _model.AddressPostalCode
        });
    }

    public void TryAddFuzzyAltDob()
    {
        if (_model.BirthDate?.Day <=
            12)
        {
            var altDob = new DateTime(
                _model.BirthDate.Value.Year,
                _model.BirthDate.Value.Day,
                _model.BirthDate.Value.Month,
                0, 0, 0,
                DateTimeKind.Unspecified
            );

            _queries.Add("FuzzyAltDob", new SearchQuery
            {
                FuzzyMatch = true,
                Given = ModelName,
                Family = _model.Family,
                Email = _model.Email,
                Gender = _model.Gender,
                Phone = _model.Phone,
                Birthdate = [$"eq{altDob:yyyy-MM-dd}"],
                AddressPostalcode = _model.AddressPostalCode,
            });
        }
    }
    
    public OrderedDictionary<string, SearchQuery> Build()
    {
        return _queries;
    }
}