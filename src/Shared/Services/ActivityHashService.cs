using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared.Util;

namespace Shared.Services;

public interface IActivityHashService
{
    string? GetUniqueSearchId();
    void StoreAlgorithmVersion(int versionNumber);
    void StoreSearchStrategy(string searchStrategy);
    void StoreQueryName(string? queryName);
    string StoreUniqueSearchIdFor(MatchPersonResult personSpecification);
    string StoreUniqueSearchIdFor(PersonSpecification personSpecification);
}

public class ActivityHashService(ILogger<ActivityHashService> logger) : IActivityHashService
{
    public string? GetUniqueSearchId()
    {
        return Activity.Current?.GetBaggageItem("SearchId");
    }

    public void StoreAlgorithmVersion(int versionNumber)
    {
        Activity.Current?.SetBaggage("AlgorithmVersion", versionNumber.ToString());
    }

    public void StoreSearchStrategy(string searchStrategy)
    {
        Activity.Current?.SetBaggage(SharedConstants.SearchStrategy.LogName, searchStrategy);
    }

    public void StoreQueryName(string? queryName)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        var value = string.IsNullOrWhiteSpace(queryName) ? null : queryName;

        try
        {
            activity.SetBaggage(SharedConstants.SearchQuery.LogName, value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Exception when storing query name '{QueryName}' in Activity baggage. Clearing query name to avoid attributing subsequent logs to the wrong query.",
                queryName
            );

            // Ensure a failed update never leaves a stale/previous query name in baggage,
            // which would misattribute later log entries to the wrong query.
            try
            {
                activity.SetBaggage(SharedConstants.SearchQuery.LogName, null);
            }
            catch (Exception clearEx)
            {
                // Highly unlikely this will happen but covered just in case
                logger.LogWarning(
                    clearEx,
                    "Exception when clearing query name in Activity baggage after a failed update."
                );
            }
        }
    }

    public string StoreUniqueSearchIdFor(MatchPersonResult personSpecification)
    {
        var given = FormatName(personSpecification.Given!);
        var family = FormatName(personSpecification.Family!);
        var gender = FormatGender(personSpecification.Gender!);
        var birthDate = FormatBirthDate(personSpecification.BirthDate!);
        var postalCode = FormatPostalCode(personSpecification.AddressPostalCode!);

        var data = PrepareDataString(given, family, birthDate, gender, postalCode);
        var hash = CreateHash(data);

        Activity.Current?.SetBaggage("SearchId", hash);
        return hash;
    }

    public string StoreUniqueSearchIdFor(PersonSpecification personSpecification)
    {
        var given = FormatName(personSpecification.Given!);
        var family = FormatName(personSpecification.Family!);
        var gender = FormatGender(personSpecification.Gender!);
        var birthDate = FormatBirthDate(personSpecification.BirthDate!);
        var postalCode = FormatPostalCode(personSpecification.AddressPostalCode!);

        var data = PrepareDataString(given, family, birthDate, gender, postalCode);
        var hash = CreateHash(data);

        Activity.Current?.SetBaggage("SearchId", hash);
        return hash;
    }

    private static string FormatGender(string inputGender)
    {
        string gender = "";
        if (string.IsNullOrWhiteSpace(inputGender))
        {
            gender = "";
        }
        else if (int.TryParse(inputGender, out _))
        {
            gender = PersonSpecificationUtils.ToGenderFromNumber(inputGender);
        }
        else
        {
            gender = inputGender!;
        }
        return gender;
    }

    private static string FormatPostalCode(string inputPostalCode)
    {
        if (string.IsNullOrWhiteSpace(inputPostalCode))
        {
            return "";
        }
        return new string(
            inputPostalCode.Where(c => !char.IsWhiteSpace(c)).ToArray()
        ).ToLowerInvariant();
    }

    private static string FormatName(string name) =>
        string.IsNullOrWhiteSpace(name) ? "" : name!.ToLowerInvariant();

    private static string FormatBirthDate(DateOnly? birthDate) =>
        birthDate is DateOnly date ? date.ToString("dd/MM/yyyy") : "";

    private static string CreateHash(string data)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(data);
        byte[] hashBytes = SHA256.HashData(bytes);

        StringBuilder builder = new();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }

        return builder.ToString();
    }

    private static string PrepareDataString(
        string given,
        string family,
        string birthDate,
        string gender,
        string postalCode
    )
    {
        return $"{given}{family}{birthDate}{gender}{postalCode}";
    }
}
