using System.Linq;

namespace Harp.Generators;

internal abstract class CSharpTemplateBase : TemplateBase
{
    protected abstract int MaxLineLength { get; }

    public void WriteSummary(string indent, params string[] fragments)
    {
        var text = string.Join(" ", fragments.Select(TemplateHelper.GetDescriptionText).Where(fragment => fragment.Length > 0));
        if (text.Length == 0) return;
        WriteDocumentationElement(indent, "summary", TemplateHelper.GetXmlText(text));
    }

    public void WriteDescriptionAttribute(string indent, params string[] fragments)
    {
        var text = string.Join(" ", fragments.Select(TemplateHelper.GetDescriptionText).Where(fragment => fragment.Length > 0));
        if (text.Length == 0) return;
        var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        WriteLine($"{indent}[Description(\"{escaped}\")]");
    }

    public void WriteDocumentation(string indent, string text)
    {
        WriteSummary(indent, text);
        WriteDescriptionAttribute(indent, text);
    }

    public void WriteDocumentationElement(string indent, string elementName, string xmlText)
    {
        WriteLine($"{indent}/// <{elementName}>");
        foreach (var line in TemplateHelper.GetDocumentationComment(indent, xmlText, MaxLineLength))
        {
            WriteLine(line);
        }
        WriteLine($"{indent}/// </{elementName}>");
    }
}
