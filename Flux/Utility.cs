namespace Flux;

public static class Utility
{
    public static void ActionOnPattern<T>(this List<T> list, Predicate<T>[] pattern, 
        // this is for when the list is modified
        Func<List<T>, int /*index*/, int /*length*/, int /*return index*/> func)
    {
        if (!list.Any() || !pattern.Any()) return;
        if (list.Count < pattern.Length) return;
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = 0; j < pattern.Length; j++)
            {
                if (!pattern[j](list[i + j]))
                {
                    i += j;
                    break;
                }

                if (j != pattern.Length - 1) continue;
                i = func(list, i, j);
                break;
            }
        }
    }

    public static void Replace<T>(this IList<T> list, int index, T replacement)
    {
        list.RemoveAt(index);
        list.Insert(index, replacement);
    }

    public static void ReplaceRange<T>(this List<T> list, int index, int count, T replacement)
    {
        list.RemoveRange(index, count);
        list.Insert(index, replacement);
    }

    public static int IndexOfWithoutLast<T>(this List<T> list, int index, Predicate<T> match, Predicate<T> lastMatch)
    {
        var nextIndex = index + 1;
        while ((nextIndex = list.FindIndex(nextIndex, match)) != -1)
        {
            if (lastMatch(list[nextIndex - 1])) return nextIndex;
            nextIndex++;
        }

        return -1;
    }

    public static string EnumerableToString<T>(this IEnumerable<T> list, Func<T, string> action)
        => string.Join("", list.Select(action));

    public static void ConstantIterator(this List<Token> tokenSet, Predicate<Token> match,
        Action<List<Token>, int> action)
    {
        int index;
        while ((index = tokenSet.FindIndex(match)) != -1)
        {
            action(tokenSet, index);
        }
    }

    public static void IncrementIterator(this List<Token> tokenSet, Predicate<Token> match,
        Func<List<Token>, int, int> func)
    {
        var index = 0;
        while ((index = tokenSet.FindIndex(index, match)) != -1)
        {
            index = func(tokenSet, index);
            if (index == -1) break;
        }
    }

    public static void IncrementIteratorMatchPair(this List<Token> tokenSet, Predicate<Token> matchOpen,
        Predicate<Token> matchClose, Func<Token[], Token> condense)
    {
        var lastIndex = tokenSet.Count - 1;
        while ((lastIndex = tokenSet.FindLastIndex(lastIndex, matchOpen)) != -1)
        {
            var index = tokenSet.FindIndex(lastIndex, matchClose);
            if (index == -1)
            {
                lastIndex--;
                continue;
            }

            var count = index - lastIndex;
            var range = tokenSet.GetRange(lastIndex + 1, count - 1).ToArray();
            tokenSet.ReplaceRange(lastIndex, count + 1, condense(range));
        }
    }
}