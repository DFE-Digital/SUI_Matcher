namespace SUI.DBS.Client.Core.Extensions;

public static class TextWriterExtensions
{
    public static void WriteAppName(this TextWriter output, string name)
    {
        string n = $"********** {name} **********";
        var p = new string('*', n.Length);

        output.WriteLine(p);
        output.WriteLine(n);
        output.WriteLine(p);
        output.WriteLine();
        output.WriteLine();
    }
}
