using Bogus;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Globalization;
using System.Reflection;

namespace SUI.Test.FakeNhsFhirApi;

public static class Generator
{
    private const string PlaceholderPhone = "#phone#";
    private const string PlaceholderBirthDate = "#birthdate#";
    private const string PlaceholderFamily = "#family#";
    private const string PlaceholderGiven = "#given#";
    private const string PlaceholderGender = "#gender#";
    private const string PlaceholderNhsId = "#nhsid#";
    private const string PlaceholderNhsId1 = "#nhsid1#";
    private const string PlaceholderNhsId2 = "#nhsid2#";

    public const string SearchApiUri = "/personal-demographics/FHIR/R4/Patient";

    public static FakeItem[] Generate(int count)
    {
        var singleMatchJsonTemplate = GetResourceText("single_match.json");
        var multiMatchJsonTemplate = GetResourceText("multi_match.json");
        var noMatchJsonTemplate = GetResourceText("no_match.json");
        
        var list = new List<FakeItem>();    

        var f = new Faker();
        for (int i = 0; i < count; i++)
        {
            var fakeItem = new FakeItem();
            fakeItem.Person.Family = f.Name.LastName();
            fakeItem.Person.Given = f.Name.FirstName();
            fakeItem.Person.Dob = f.Date.BetweenDateOnly(new DateOnly(1995, 1, 1), new DateOnly(2016, 1, 1)).ToString("yyyy-MM-dd");
            fakeItem.Person.NhsId = f.Random.AlphaNumeric(10).ToUpper();
            fakeItem.Person.Email = f.Internet.Email();
            fakeItem.Person.Phone = f.Phone.PhoneNumber("(01###) ### ###");
            fakeItem.Person.Gender = f.Random.Bool() ? "male" : "female";



            var o = f.Random.Number(1, 3);
            if (o == 1) // single match
            {
                fakeItem.MatchType = "single";
                fakeItem.ResponseJson = MergeTemplate(singleMatchJsonTemplate, fakeItem.Person, f);
            }
            else if (o == 2) // multi
            {
                fakeItem.MatchType = "multi";
                fakeItem.ResponseJson = MergeTemplate(multiMatchJsonTemplate, fakeItem.Person, f);
            }
            else // no match
            {
                fakeItem.MatchType = "none";
                fakeItem.ResponseJson = noMatchJsonTemplate;
            }

            list.Add(fakeItem);
        }

        return [.. list];  
    }

    public static string GetResourceText(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream($"{nameof(SUI)}.{nameof(Test)}.{nameof(FakeNhsFhirApi)}.Resources.{name}")
                              ?? throw new InvalidOperationException("Resource not found.");
        using StreamReader reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return content;
    }

    public static string MergeTemplate(string template, FakePerson person, Faker faker)
    {
        return template
            .Replace(PlaceholderGiven, person.Given)
            .Replace(PlaceholderFamily, person.Family)
            .Replace(PlaceholderGender, person.Gender)
            .Replace(PlaceholderPhone, person.Phone)
            .Replace(PlaceholderBirthDate, person.Dob)
            .Replace(PlaceholderNhsId, person.NhsId)
            .Replace(PlaceholderNhsId1, person.NhsId)
            .Replace(PlaceholderNhsId2, faker.Random.AlphaNumeric(10).ToUpper());
    }

    public static void WriteCsv<T>(List<T> records, string filename)
    {
        using var writer = new StreamWriter(filename);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(records);
    }


    public static void WriteTestData(string baseDirectory, FakeItem[] data, int port)
    {
        var @base = Path.Combine(baseDirectory, "sui-e2e");
        ResetDirectory(@base);

        var batches = SplitIntoRandomBatches(data.ToList(), 5, 35);

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var dir = Path.Combine(@base, $"sample_{i}");
            Directory.CreateDirectory(dir);
            var baselineData = batch.Select(x => new
            {
                x.Person.Given,
                x.Person.Family,
                x.Person.Dob,
                x.Person.Gender,
                x.Person.Email,
                x.Person.Phone,
                __Expected_NHS_ID = x.Person.NhsId,
                __Expected_Match_Type = x.MatchType,
                __curl = $"curl \"http://localhost:{port}{SearchApiUri}?given={x.Person.Given}&family={x.Person.Family}&birthdate=eq{x.Person.Dob}\""

            }).ToList();

            var inputSampleData = batch.Select(x => x.Person).ToList();

            WriteCsv(baselineData, Path.Combine(dir, $"baseline_{i}.csv"));
            WriteCsv(inputSampleData, Path.Combine(dir, $"sample_input_{i}.csv"));
        }
    }

    private static void ResetDirectory(string @base)
    {
        if (Directory.Exists(@base))
        {
            Directory.Delete(@base, true);
        }
        Directory.CreateDirectory(@base);
    }

    public static List<List<T>> SplitIntoRandomBatches<T>(List<T> items, int minBatchSize, int maxBatchSize)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("The list cannot be null or empty.");
        if (minBatchSize <= 0 || maxBatchSize < minBatchSize)
            throw new ArgumentException("Batch size parameters must be valid.");

        var random = new Random();
        var shuffledItems = items.OrderBy(_ => random.Next()).ToList(); // Shuffle the list randomly
        var batches = new List<List<T>>();

        int index = 0;
        while (index < shuffledItems.Count)
        {
            int batchSize = random.Next(minBatchSize, maxBatchSize + 1);
            batchSize = Math.Min(batchSize, shuffledItems.Count - index); // Ensure we don't go out of bounds

            batches.Add(shuffledItems.GetRange(index, batchSize));
            index += batchSize;
        }

        return batches;
    }
}


public class FakeItem
{
    public FakePerson Person { get; set; } = new();
    public string ResponseJson { get; set; }
    public string MatchType { get; set; }

}


public class FakePerson
{
    [Name("GivenName")]
    public string Given { get; set; }

    [Name("Surname")]
    public string Family { get; set; }

    [Name("DOB")]
    public string Dob { get; set; }

    [Name("Phone")]
    public string Phone { get; set; }

    [Name("Email")]
    public string Email { get; set; }

    [Ignore]
    public string NhsId { get; set; }

    [Name("Gender")]
    public string Gender { get; set; }
}
