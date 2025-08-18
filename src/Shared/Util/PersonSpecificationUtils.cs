namespace Shared.Util;

public static class PersonSpecificationUtils
{
    /// <summary>
    /// Converts a number representing gender into a string representation.
    /// </summary>
    /// <param name="value">number as string</param>
    /// <param name="defaultValue"></param>
    /// <returns>a gender if known, default value if it is not a known number. Or empty string if there is no value</returns>
    public static string ToGenderFromNumber(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        value = value.Trim();
        return value switch
        {
            "0" => "not known",
            "1" => "male",
            "2" => "female",
            "9" => "not specified",
            _ => "unknown"
        };
    }

    public static string GetAgeGroup(DateOnly birthDate)
    {
        var dateOnlyNow = DateOnly.FromDateTime(DateTime.Now);
        var age = dateOnlyNow.Year - birthDate.Year;
        if (dateOnlyNow.DayOfYear < birthDate.DayOfYear)
        {
            age--;
        }

        return age switch
        {
            < 1 => "Less than 1 year",
            <= 3 => "1-3 years",
            <= 7 => "4-7 years",
            <= 11 => "8-11 years",
            <= 15 => "12-15 years",
            <= 18 => "16-18 years",
            _ => "Over 18 years"
        };
    }
}