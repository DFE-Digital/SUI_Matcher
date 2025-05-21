using System.Diagnostics.CodeAnalysis;

namespace Shared.Util;

[ExcludeFromCodeCoverage(Justification = "Rule class is a simple utility class")]
public static class Rule
{
    public static void Assert(bool assertion, string message)
    {
        if (assertion)
        {
            return;
        }

        Console.Error.WriteLine(message);
        Environment.Exit(1);
    }
}