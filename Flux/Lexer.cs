using System.Collections.ObjectModel;

namespace Flux;

public static class Lexer
{
    public static readonly Separate SeparateToken = new();
    public static readonly Tab TabToken = new();
    public static readonly PublicToken PublicToken = new();
    public static readonly IntegerType IntegerType = new();

    public static readonly ReadOnlyDictionary<char, Token> TokenMatcher = new(new Dictionary<char, Token>
    {
        { '\t', TabToken },
        { ' ', new Space() },
        { '`', new CommentStart() },
        { ';', SeparateToken },
        { '(', new OpenBracket() },
        { ')', new CloseBracket() },
        { '[', new OpenSquareBracket() },
        { ']', new CloseSquareBracket() },
        { '{', new OpenCurlyBracket() },
        { '}', new CloseCurlyBracket() },
        { '"', new DoubleQuote() },
        { '\'', new SingleQuote() },
        { '\\', new Escape() },
        { '\n', new NewLine() },
    });

    public static readonly ReadOnlyDictionary<string, CharacterGroup> CharacterGroupMatcher = new(
        new Dictionary<string, CharacterGroup>
        {
            { "pub", PublicToken },
            { "public", PublicToken },
            { "private", new PrivateToken() },
            { "int", IntegerType },
            { "integer", IntegerType }
        });

    public static readonly Dictionary<char, Character> CharacterMatcher = new();

    public static TokenFile Tokenize(string file)
    {
        // initial parsing
        var tokenSet = File.ReadAllText(file).Replace("\r", "")
            .Select(character =>
            {
                if (TokenMatcher.TryGetValue(character, out var token)) return token;
                if (CharacterMatcher.TryGetValue(character, out var charMatch)) return charMatch;
                var characterToAdd = new Character(character);
                CharacterMatcher.Add(character, characterToAdd);
                return characterToAdd;
            })
            .ToList();

        // characters to character groups
        tokenSet.ConstantIterator(token => token is Character, (list, i) =>
        {
            var count = list.FindIndex(i, token => token is not Character) - i;
            var group = list.GetRange(i, count);

            list.ReplaceRange(i, count, new CharacterGroup(
                string.Join("", group.Select(c => ((Character) c).Data))));
        });

        // replace 4 spaces with a tab
        tokenSet.IncrementIterator(token => token is Space, (list, i) =>
        {
            if (list.Count - i < 4) return -1;
            var range = list.GetRange(i, 4);
            if (range.Any(token => token is not Space)) return i + 1;

            list.ReplaceRange(i, 4, TabToken);
            return i;
        });

        // stringify
        tokenSet.IncrementIterator(token => token is SingleQuote or DoubleQuote, (list, i) =>
        {
            var closing = list.IndexOfWithoutLast(i, token => list[i] is SingleQuote
                    ? token is SingleQuote
                    : token is DoubleQuote,
                token => token is not Escape);

            if (closing == -1) return i + 1;

            var count = closing - i;
            var str = list.GetRange(i + 1, count - 1);
            list.ReplaceRange(i, count + 1,
                new CharacterString(str.EnumerableToString(token => token.Data)));
            return i;
        });

        // commentify
        tokenSet.ConstantIterator(token => token is CommentStart, (list, i) =>
        {
            var count = list.FindIndex(i, token => token is NewLine) - i;
            var range = list.GetRange(i + 1, count - 1);
            list.ReplaceRange(i, count,
                new Comment(range.EnumerableToString(token => token.Data)));
        });

        // bye bye whitespace
        tokenSet.RemoveAll(token => token is Space or Tab);

        // bye bye whitespace pt 2
        tokenSet.ConstantIterator(token => token is NewLine,
            (list, i) => list.Replace(i, SeparateToken));

        // remove excess separators
        tokenSet.IncrementIterator(token => token is Separate, (list, i) =>
        {
            if (list[i + 1] is not Separate) return i + 1;
            list.RemoveAt(i + 1);
            return i;
        });

        // remove excess separators pt 2
        tokenSet.IncrementIterator(token => token is OpenCurlyBracket or CloseCurlyBracket, (list, i) =>
        {
            if (list.Count - 1 > i && list[i + 1] is Separate) list.RemoveAt(i + 1);
            if (i == 0 || list[i - 1] is not Separate) return i + 1;
            list.RemoveAt(i - 1);
            return i;
        });

        // elevate character groups
        for (var i = 0; i < tokenSet.Count; i++)
        {
            if (tokenSet[i] is not CharacterGroup charGroup) continue;
            var data = charGroup.Data.ToLower();
            if (!CharacterGroupMatcher.TryGetValue(data, out var replacementGroup)) continue;
            tokenSet[i] = replacementGroup;
        }

        // pair ( and )
        tokenSet.IncrementIteratorMatchPair(token => token is OpenBracket, token => token is CloseBracket,
            tokens => new ParaSet(tokens));

        // pair { and }
        tokenSet.IncrementIteratorMatchPair(token => token is OpenCurlyBracket, token => token is CloseCurlyBracket,
            tokens => new Body(tokens));

        var functionMatch = new Predicate<Token>[]
        {
            token => token is TypeToken,
            token => token is CharacterGroup,
            token => token is ParaSet,
            token => token is Body
        };
        
        // create function tokens
        tokenSet.ActionOnPattern(functionMatch, (list, i, _) =>
        {
            list.ReplaceRange(i, 4,
                new Function((TypeToken) list[i], (CharacterGroup) list[i + 1], (ParaSet) list[i + 2],
                    (Body) list[i + 3]));
            return i;
        });

        return new TokenFile(tokenSet.ToArray());
    }
}