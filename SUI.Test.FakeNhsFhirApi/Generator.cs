using Bogus;
using CsvHelper;
using System.Globalization;
using System.Reflection;

namespace SUI.Test.FakeNhsFhirApi;

public static class Generator
{
    public const string SearchApiUri = "/personal-demographics/FHIR/R4/Patient";
    private const string PlaceholderBirthDate = "#birthdate#";
    private const string PlaceholderFamily = "#family#";
    private const string PlaceholderGender = "#gender#";
    private const string PlaceholderGiven = "#given#";
    private const string PlaceholderNhsId = "#nhsid#";
    private const string PlaceholderNhsId1 = "#nhsid1#";
    private const string PlaceholderNhsId2 = "#nhsid2#";
    private const string PlaceholderPhone = "#phone#";
    private const string PLaceholderScore = "#score#";

    public static FakeItem[] Generate(int count)
    {
        var singleMatchJsonTemplate = GetResourceText("single_match.json");
        var multiMatchJsonTemplate = GetResourceText("multi_match.json");
        var noMatchJsonTemplate = GetResourceText("no_match.json");

        var faker = new Faker();

        var singleMatchHighs = Enumerable.Range(0, CalcSubsetSize(count, 90)).Select(x => CreateFakeItem(singleMatchJsonTemplate, faker, "single_match (high)", faker.Random.Double(0.95, 1))).ToArray();
        var singleMatchLows = Enumerable.Range(0, CalcSubsetSize(count, 5)).Select(x => CreateFakeItem(singleMatchJsonTemplate, faker, "single_match (low)", faker.Random.Double(0.85, 0.949999999))).ToArray();
        var multiMatch = Enumerable.Range(0, CalcSubsetSize(count, 2)).Select(x => CreateFakeItem(multiMatchJsonTemplate, faker, "multi_match", 0)).ToArray();
        var noMatch = Enumerable.Range(0, CalcSubsetSize(count, 3)).Select(x => CreateFakeItem(noMatchJsonTemplate, faker, "no_match", 0)).ToArray();

        FakeItem[] fullset = [.. singleMatchHighs, .. singleMatchLows, .. multiMatch, .. noMatch];

        var randomized = new Randomizer().Shuffle(fullset).ToArray();

        return randomized;
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

    public static string MergeTemplate(string template, FakeItem fakeItem, FakePerson person, Faker faker)
    {
        return template
            .Replace(PlaceholderGiven, person.Given)
            .Replace(PlaceholderFamily, person.Family)
            .Replace(PlaceholderGender, person.Gender)
            .Replace(PlaceholderPhone, person.Phone)
            .Replace(PlaceholderBirthDate, person.Dob)
            .Replace(PlaceholderNhsId, person.NhsId)
            .Replace(PlaceholderNhsId1, person.NhsId)
            .Replace(PLaceholderScore, fakeItem.Score.ToString())
            .Replace(PlaceholderNhsId2, faker.Random.AlphaNumeric(10).ToUpper());
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

        WriteBatch(port, @base, "full", [.. data]);

        var batches = SplitIntoRandomBatches(data.ToList(), 5, 35);
        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            WriteBatch(port, @base, i.ToString(), batch);
        }
    }

    private static int CalcSubsetSize(int total, int percentage) => (int)Math.Floor(total * ((double)percentage / 100));

    private static FakeItem CreateFakeItem(string jsonTemplate, Faker faker, string matchType, double score)
    {
        var fakeItem = new FakeItem();
        fakeItem.Person.Family = faker.Name.LastName();
        fakeItem.Person.Given = faker.Name.FirstName();
        fakeItem.Person.Dob = faker.Date.BetweenDateOnly(new DateOnly(1995, 1, 1), new DateOnly(2016, 1, 1)).ToString("yyyy-MM-dd");
        fakeItem.Person.NhsId = faker.Random.AlphaNumeric(10).ToUpper();
        fakeItem.Person.Email = faker.Internet.Email();
        fakeItem.Person.Phone = faker.Phone.PhoneNumber("(01###) ### ###");
        fakeItem.Person.Gender = faker.Random.Bool() ? "male" : "female";
        fakeItem.MatchType = matchType;
        fakeItem.Score = score;

        fakeItem.ResponseJson = MergeTemplate(jsonTemplate, fakeItem, fakeItem.Person, faker);

        return fakeItem;
    }
    private static void ResetDirectory(string @base)
    {
        if (Directory.Exists(@base))
        {
            Directory.Delete(@base, true);
        }
        Directory.CreateDirectory(@base);
    }

    private static void WriteBatch(int port, string @base, string suffix, List<FakeItem> batch)
    {
        var dir = Path.Combine(@base, $"sample_{suffix}");
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
            __Expected_Score = x.Score.ToString(),
            __curl = $"curl \"http://localhost:{port}{SearchApiUri}?given={x.Person.Given}&family={x.Person.Family}&birthdate=eq{x.Person.Dob}\""
        }).ToList();

        var inputSampleData = batch.Select(x => x.Person).ToList();

        WriteCsv(baselineData, Path.Combine(dir, $"baseline_{suffix}.csv"));
        WriteCsv(inputSampleData, Path.Combine(dir, $"sample_input_{suffix}.csv"));
    }
}
