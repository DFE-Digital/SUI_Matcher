namespace Shared.Util;

public static class CsvUtils
{
    public static string WrapInputForCsv(string[]? input)
    {
        if (input == null || input.Length == 0)
        {
            return "-";
        }

        string rawInput = string.Join(" ", input);

        if (rawInput.Contains(',') || rawInput.Contains('"'))
        {
            string escapedInput = rawInput.Replace("\"", "\"\"");
            return $"\"{escapedInput}\"";
        }

        return rawInput;

    }
}