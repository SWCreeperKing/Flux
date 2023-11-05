using System.Text;

namespace Flux;

public static class Parser
{
    public static string ParseTokenFile(TokenFile file)
    {
        StringBuilder sb = new();
        List<string> importsList = new();

        foreach (var function in file.Tokens.OfType<Function>())
        {
            sb.Append(TypeToString(function.Type)).Append(' ').Append(function.Name.Data)
                .Append(ParseFuncParameters(function.Parameters))
                .Append(ParseBody(function.Body, 1, out var importsRaw));
            importsList.AddRange(importsRaw);
        }

        var imports = string.Join("\n", importsList.Union(importsList).Select(import => $"#include <{import}>"));
        return $"{imports}\nusing namespace std;\n\n{sb}";
    }

    public static string ParseFuncParameters(ParaSet paraSet)
    {
        return "()";
    }

    public static string ParseBody(Body body, int indent, out string[] imports)
    {
        var indentStringMinus = string.Join("", Enumerable.Repeat(' ', 4 * (indent - 1)));
        var indentString = string.Join("", Enumerable.Repeat(' ', 4 * indent));
        List<string> importsList = new();

        StringBuilder sb = new();
        List<Token[]> lines = new();
        var length = body.Tokens.Length;
        for (int i = 0, j = 0; i < length; i++)
        {
            if (i + 1 >= length)
            {
                lines.Add(body.Tokens[j..]);
                break;
            }
            
            if (body.Tokens[i] is not Separate) continue;
            lines.Add(body.Tokens[j..i]);
            j = i + 1;
        }

        foreach (var line in lines)
        {
            var text = ParseLine(out var importsRaw, line);
            if (text is "") continue;
            sb.Append(indentString).Append(text).Append('\n');
            importsList.AddRange(importsRaw);
        }

        imports = importsList.Union(importsList).ToArray();
        var bodyString = sb.ToString();
        sb.Clear();
        return sb.Append('\n').Append(indentStringMinus).Append("{\n").Append(bodyString)
            .Append(indentStringMinus).Append('}').ToString();
    }

    public static string ParseLine(out string[] imports, params Token[] tokens)
    {
        switch (tokens[0])
        {
            case Comment comment:
                imports = Array.Empty<string>();
                return $"// {comment.Data}";
            case CharacterGroup charGroup when charGroup.Data.ToLower() == "print":
                imports = new[] { "iostream" };
                return $"cout << {string.Join(" << ", tokens[1..].Select(ParseGenericToken))};";
            case CharacterGroup charGroup when charGroup.Data.ToLower() == "return":
                imports = Array.Empty<string>();
                return $"return {string.Join("", tokens[1..].Select(ParseGenericToken))};";
            default:
                imports = Array.Empty<string>();
                return "";
        }
    }

    public static string ParseGenericToken(Token token)
        => token switch
        {
            CharacterString charStr => $"\"{charStr.Data}\"",
            _ => token.Data
        };

    public static string TypeToString(TypeToken type)
        => type switch
        {
            IntegerType => "int"
        };
}