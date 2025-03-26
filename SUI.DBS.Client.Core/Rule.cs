namespace SUI.DBS.Client.Core;

public static class Rule
{
    public static void Assert(bool assertion, string message)
    {
        if (!assertion)
        {
            throw new Exception(message);
        }
    }
}
