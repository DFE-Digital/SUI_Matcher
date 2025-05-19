using System.Diagnostics.CodeAnalysis;

namespace SUI.Client.Core.Extensions;

[ExcludeFromCodeCoverage(Justification = "This is just a CLI WriteLine extension")]
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