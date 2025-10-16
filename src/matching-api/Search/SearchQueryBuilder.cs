using System.Text.RegularExpressions;

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
    private string[]? ModelNames => _model.Given?.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    private string? FamilyName => _model.Family is not null ? Regex.Replace(_model.Family, @"\s\(.*\)", string.Empty, RegexOptions.Compiled, TimeSpan.FromMilliseconds(300)) : null;
    private string[] DobRange =>
    [
        "ge" + _model.BirthDate!.Value.AddMonths(-_dobRange).ToString(SharedConstants.SearchQuery.DateFormat),
        "le" + _model.BirthDate.Value.AddMonths(_dobRange).ToString(SharedConstants.SearchQuery.DateFormat)
    ];
    private string[] Dob => ["eq" + _model.BirthDate!.Value.ToString(SharedConstants.SearchQuery.DateFormat)];

    /// <summary>
    /// See <see href="https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient"/>
    /// for postcode search details and using wildcard.
    /// </summary>
    /// <returns></returns>
    private string? PostcodeWildcard()
    {
        if (!string.IsNullOrEmpty(_model.AddressPostalCode))
        {
            var postcode = _model.AddressPostalCode.Length > 2
                ? string.Concat(_model.AddressPostalCode.AsSpan(0, 2), "*")
                : _model.AddressPostalCode;

            return postcode;
        }

        return null;
    }

    public void AddNonFuzzyGfd(bool preprocessNames = false)
    {
        _queries.Add("NonFuzzyGFD", new SearchQuery()
        {
            ExactMatch = false,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Birthdate = Dob,
            History = true
        });
    }

    public void AddNonFuzzyGfdPostcode(bool preprocessNames = false)
    {
        _queries.Add("NonFuzzyGFDPostcode", new SearchQuery()
        {
            ExactMatch = false,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Birthdate = Dob,
            AddressPostalcode = _model.AddressPostalCode,
            History = true
        });
    }

    public void AddNonFuzzyGfdRange(bool preprocessNames = false)
    {
        _queries.Add("NonFuzzyGFDRange", new SearchQuery()
        {
            ExactMatch = false,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Birthdate = DobRange,
            History = true
        });
    }

    public void AddNonFuzzyGfdRangePostcode(bool usePostcodeWildcard = false, bool preprocessNames = false)
    {
        var name = usePostcodeWildcard ? "NonFuzzyGFDRangePostcodeWildcard" : "NonFuzzyGFDRangePostcode";
        _queries.Add(name, new SearchQuery()
        {
            ExactMatch = false,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Birthdate = DobRange,
            AddressPostalcode = usePostcodeWildcard ? PostcodeWildcard() : _model.AddressPostalCode,
            History = true
        });
    }

    public void AddNonFuzzyAll(bool preprocessNames = false)
    {
        _queries.Add("NonFuzzyAll", new SearchQuery()
        {
            ExactMatch = false,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Email = _model.Email,
            Gender = _model.Gender,
            Phone = _model.Phone,
            Birthdate = Dob,
            AddressPostalcode = _model.AddressPostalCode,
            History = true
        });
    }

    public void AddNonFuzzyAllPostcodeWildcard(bool preprocessNames = false)
    {
        _queries.Add("NonFuzzyAllPostcodeWildcard", new SearchQuery()
        {
            ExactMatch = false,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Email = _model.Email,
            Gender = _model.Gender,
            Phone = _model.Phone,
            Birthdate = Dob,
            AddressPostalcode = PostcodeWildcard(),
            History = true
        });
    }

    public void AddFuzzyGfd(bool preprocessNames = false)
    {
        _queries.Add("FuzzyGFD", new SearchQuery()
        {
            FuzzyMatch = true,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Birthdate = Dob
        });
    }

    public void AddFuzzyAll(bool preprocessNames = false)
    {
        _queries.Add("FuzzyAll", new SearchQuery()
        {
            FuzzyMatch = true,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Email = _model.Email,
            Gender = _model.Gender,
            Phone = _model.Phone,
            Birthdate = Dob,
            AddressPostalcode = _model.AddressPostalCode
        });
    }

    public void AddFuzzyGfdPostcodeWildcard(bool preprocessNames = false)
    {
        _queries.Add("FuzzyGFDPostcodeWildcard",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = preprocessNames ? ModelNames : ModelName,
                Family = preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                AddressPostalcode = PostcodeWildcard()
            });
    }

    public void AddFuzzyGfdRangePostcodeWildcard(bool preprocessNames = false)
    {
        _queries.Add("FuzzyGFDRangePostcodeWildcard",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = preprocessNames ? ModelNames : ModelName,
                Family = preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                AddressPostalcode = PostcodeWildcard()
            });
    }

    public void AddFuzzyGfdRangePostcode(bool preprocessNames = false)
    {
        _queries.Add("FuzzyGFDRangePostcode",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = preprocessNames ? ModelNames : ModelName,
                Family = preprocessNames ? FamilyName : _model.Family,
                Birthdate = DobRange,
                AddressPostalcode = _model.AddressPostalCode
            });
    }

    public void AddFuzzyGfdRange(bool preprocessNames = false)
    {
        _queries.Add("FuzzyGFDRange",
            new SearchQuery()
            {
                FuzzyMatch = true,
                Given = ModelName,
                Family = FamilyName,
                Birthdate = DobRange
            });
    }

    public void AddExactGfd(bool preprocessNames = false)
    {
        _queries.Add("ExactGFD", new SearchQuery()
        {
            ExactMatch = true,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Birthdate = Dob
        });
    }

    public void AddExactAll(bool preprocessNames = false)
    {
        _queries.Add("ExactAll", new SearchQuery()
        {
            ExactMatch = true,
            Given = preprocessNames ? ModelNames : ModelName,
            Family = preprocessNames ? FamilyName : _model.Family,
            Email = _model.Email,
            Gender = _model.Gender,
            Phone = _model.Phone,
            Birthdate = Dob,
            AddressPostalcode = _model.AddressPostalCode
        });
    }

    public void TryAddFuzzyAltDob(bool preprocessNames = false)
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
                Given = preprocessNames ? ModelNames : ModelName,
                Family = preprocessNames ? FamilyName : _model.Family,
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