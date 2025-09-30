namespace Shared.Util;

public static class NhsNumberValidator
{
    private const int NhsNumberLength = 10;
    private const int RemainderConstant = 11;
    private static readonly int[] Weightings = [10, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Validate an NHS number
    /// </summary>
    /// <param name="number">The NHS number you wish to validate</param>
    /// <returns>bool - validation result</returns>
    /// <exception cref="ArgumentNullException">Throws if the number provided is null</exception>
    public static bool Validate(string? number)
    {
        _ = number ?? throw new ArgumentNullException(nameof(number));

        if (IsAnInvalidLength(number)) return false;

        var characters = ConvertStringToDigits(number);
        var checkSum = ExtractCheckDigit(characters);
        var weightedTotal = CalculateWeightedTotal(characters);
        var remainder = CalculateRemainder(weightedTotal);
        return RemainderAndChecksumMatch(remainder, checkSum);
    }

    private static bool IsAnInvalidLength(string number) => number.Length != NhsNumberLength;

    private static List<int> ConvertStringToDigits(string number)
    {
        var characters = number.Select(x => new string(x, 1)).ToList();
        return characters.Select(int.Parse).ToList();
    }

    private static int ExtractCheckDigit(List<int> characters) => characters[NhsNumberLength - 1];

    private static int CalculateWeightedTotal(List<int> characters)
    {
        var position = 0;
        var lastCharacter = characters.Count;

        return characters.TakeWhile(_ => position != lastCharacter - 1)
            .Sum(character => character * Weightings[position++]);
    }

    private static int CalculateRemainder(int weightedTotal) => weightedTotal % RemainderConstant;

    private static bool RemainderAndChecksumMatch(int remainder, int checkDigit)
    {
        var actual = remainder == 0 ? 0 : RemainderConstant - remainder;
        return actual == checkDigit;
    }
}