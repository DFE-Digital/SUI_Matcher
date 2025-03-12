using System.Globalization;

namespace SUI.Core;

public static class ExtensionMethods
{
    public static DateOnly? ToDateOnly(this string? value, string format) 
        => value != null && DateOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;

    public static DateOnly? ToDateOnly(this string? value, params string[] formats)
    {
        if (value != null)
        {
            foreach (var format in formats)
            {
                var rv = ToDateOnly(value, format);
                if (rv != null)
                {
                    return rv;
                }
            }
        }
        return null;
    }
}
