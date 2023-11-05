using System.Text;
using Flux;

Console.OutputEncoding = Encoding.UTF8;

var file = Lexer.Tokenize("Code/Test.lux");
PrintTokenGroup(file);

void PrintTokenGroup(TokenGroup group, int indent = 0)
{
    string MakeIndent(int indent = 0, bool main = false)
    {
        StringBuilder sb = new();
        for (var i = 0; i < indent + 1; i++)
        {
            sb.Append(main ? "  \u2502 " : "    ");
        }

        return sb.ToString();
    }

    if (group is Function func)
    {
        Console.WriteLine($"{MakeIndent(indent - 1, true)}{func.GetType().ToString().Replace("Flux.", "")}   [{func.Name.Data}]");
    }
    else
    {
        Console.WriteLine($"{MakeIndent(indent - 1, true)}{group.GetType().ToString().Replace("Flux.", "")}");
    }

    foreach (var token in group.Tokens)
    {
        switch (token)
        {
            case TokenGroup tokenGroup:
                PrintTokenGroup(tokenGroup, indent + 1);
                continue;
            case Separate:
                Console.WriteLine(
                    $"{MakeIndent(indent, true)}\u25c1\u2505\u2505\u2505\u2505\u2505\u2505\u2505\u2505\u2505\u2505");
                continue;
            default:
                Console.WriteLine($"{MakeIndent(indent, true)}{token.GetType().ToString().Replace("Flux.", "")}");
                Console.WriteLine($"{MakeIndent(indent, true)}{MakeIndent()}\u2515 {token.Data.Replace("\n", "\\n")}");
                break;
        }
    }

    if (!Directory.Exists("CompileOutput"))
    {
        Directory.CreateDirectory("CompileOutput");
    }

    File.WriteAllText("CompileOutput/Test.cpp", Parser.ParseTokenFile(file));
}