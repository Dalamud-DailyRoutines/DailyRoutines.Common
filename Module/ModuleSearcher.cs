using System.Text;
using DailyRoutines.Common.Module.Abstractions;
using TinyPinyin;

namespace DailyRoutines.Common.Module;

public sealed class ModuleSearcher
{
    private static readonly SearchWeight TitleWeight = new(400, 24, 18, 16, 200, 3);
    private static readonly SearchWeight NameWeight  = new(320, 20, 14, 12, 160, 4);
    private static readonly SearchWeight MetaWeight  = new(200, 14, 10, 8, 100, 5);

    private readonly SearchEntry[] entries;

    private string?      cachedQuery;
    private ModuleBase[] cachedResult = [];

    public ModuleSearcher
    (
        IEnumerable<ModuleBase>                  modules,
        Func<ModuleBase, IEnumerable<string?>?>? extraSegmentsSelector = null
    )
    {
        ArgumentNullException.ThrowIfNull(modules);

        List<SearchEntry> builtEntries = [];

        foreach (var module in modules)
        {
            if (module == null) continue;

            builtEntries.Add
            (
                CreateEntry
                (
                    module,
                    extraSegmentsSelector?.Invoke(module)
                )
            );
        }

        entries = [.. builtEntries];
    }

    public ModuleBase[] Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            cachedQuery  = null;
            cachedResult = [];
            return cachedResult;
        }

        var trimmed = query.Trim();

        if (Compact(trimmed).Length < 2)
        {
            cachedQuery  = NormalizeText(trimmed);
            cachedResult = [];
            return cachedResult;
        }

        var normalizedQuery = NormalizeText(trimmed);
        if (string.Equals(cachedQuery, normalizedQuery, StringComparison.Ordinal))
            return cachedResult;

        var terms = ParseTerms(trimmed);

        if (terms.Count == 0)
        {
            cachedQuery  = normalizedQuery;
            cachedResult = [];
            return cachedResult;
        }

        List<(ModuleBase Module, int Score)> matches = [];

        foreach (var entry in entries)
        {
            if (!TryScore(entry, terms, out var score)) continue;
            matches.Add((entry.Module, score));
        }

        matches.Sort
        (static (left, right) =>
            {
                var scoreComparison = right.Score.CompareTo(left.Score);
                return scoreComparison != 0
                           ? scoreComparison
                           : string.Compare(left.Module.Info.Title, right.Module.Info.Title, StringComparison.OrdinalIgnoreCase);
            }
        );

        cachedQuery  = normalizedQuery;
        cachedResult = [.. matches.Take(30).Select(static x => x.Module)];
        return cachedResult;
    }

    private static SearchEntry CreateEntry(ModuleBase module, IEnumerable<string?>? extraSegments)
    {
        var info       = module.Info;
        var moduleType = module.GetType();

        return new
        (
            module,
            [
                CreateKey([info.Title],                         TitleWeight),
                CreateKey([module.ModuleName, moduleType.Name], NameWeight),
                CreateKey
                (
                    [
                        string.Join(" / ", info.Author),
                        info.Description,
                        string.Join(" / ", info.ModulesPrerequisite),
                        string.Join(" / ", info.ModulesRecommend),
                        string.Join(" / ", info.ModulesConflict),
                        string.Join(" / ", module.PrecedingModules?.Select(static x => x.ModuleName) ?? []),
                        string.Join(" / ", module.RecommendModules?.Select(static x => x.ModuleName) ?? []),
                        string.Join(" / ", module.ConflictModules?.Select(static x => x.ModuleName)  ?? []),
                        info.ReportURL,
                        string.Join(" / ", info.SupportUrls),
                        string.Join(" / ", info.PreviewImageURL)
                    ],
                    MetaWeight,
                    extraSegments
                )
            ]
        );
    }

    private static bool TryScore(SearchEntry entry, IReadOnlyList<SearchTerm> terms, out int score)
    {
        score = 0;

        foreach (var term in terms)
        {
            if (!TryScoreTerm(entry.Keys, term, out var termScore))
            {
                score = 0;
                return false;
            }

            score += termScore;
        }

        score += terms.Count * 24;
        return true;
    }

    private static bool TryScoreTerm(SearchKey[] keys, SearchTerm term, out int score)
    {
        score = 0;

        foreach (var key in keys)
            score = Math.Max(score, ScoreKey(key, term));

        return score > 0;
    }

    private static int ScoreKey(SearchKey key, SearchTerm term)
    {
        if (string.IsNullOrEmpty(term.Compact)) return 0;

        var query = term.Compact;

        if (!string.IsNullOrEmpty(key.Text) &&
            key.Text.Equals(term.Normalized, StringComparison.Ordinal))
            return key.Weight.BaseScore * 4 + query.Length * 10;

        if (!string.IsNullOrEmpty(key.Text))
        {
            var textScore = FuzzyMatch(key.Text, query, key.Weight);
            if (textScore > 0) return textScore;
        }

        var bestScore = 0;

        if (!string.IsNullOrEmpty(key.CompactText))
        {
            var compactScore = FuzzyMatch(key.CompactText, query, key.Weight);
            bestScore = Math.Max(bestScore, compactScore);
        }

        if (!string.IsNullOrEmpty(key.InitialismText))
        {
            var initScore = FuzzyMatch
            (
                key.InitialismText,
                query,
                key.Weight with { BaseScore = key.Weight.InitialismBase }
            );
            bestScore = Math.Max(bestScore, initScore);
        }

        if (bestScore == 0 && query.Length >= 6 && !string.IsNullOrEmpty(key.CompactText))
            bestScore = Math.Max(bestScore, TryUnorderedMatch(key.CompactText, query, key.Weight));

        if (bestScore == 0 && query.Length >= 3 && !string.IsNullOrEmpty(key.CompactText))
            bestScore = Math.Max(bestScore, ApproximateMatch(key.CompactText, query, key.Weight));

        return bestScore;
    }

    private static SearchKey CreateKey(IEnumerable<string?> primarySegments, SearchWeight weight, IEnumerable<string?>? extraSegments = null)
    {
        var segments = CollectSegments(primarySegments, extraSegments);
        if (segments.Length == 0)
            return new(string.Empty, string.Empty, string.Empty, weight);

        var textBuilder       = new StringBuilder();
        var compactBuilder    = new StringBuilder();
        var initialismBuilder = new StringBuilder();

        foreach (var segment in segments)
        {
            if (textBuilder.Length > 0)
                textBuilder.Append('\n');

            textBuilder.Append(segment);
            AppendCompact(compactBuilder, segment);
            AppendAsciiInitialism(initialismBuilder, segment);

            var pinyinCompact = PinyinHelper.GetPinyin(segment, string.Empty).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(pinyinCompact) &&
                !segment.Equals(pinyinCompact, StringComparison.OrdinalIgnoreCase))
                AppendCompact(compactBuilder, pinyinCompact);

            var pinyinWords = PinyinHelper.GetPinyin(segment);
            if (!string.IsNullOrWhiteSpace(pinyinWords))
                AppendWordInitialism(initialismBuilder, pinyinWords);
        }

        return new(textBuilder.ToString(), compactBuilder.ToString(), initialismBuilder.ToString(), weight);
    }

    private static string[] CollectSegments(IEnumerable<string?> primarySegments, IEnumerable<string?>? extraSegments = null)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (var text in primarySegments)
            AddSegment(result, text);

        if (extraSegments != null)
        {
            foreach (var text in extraSegments)
                AddSegment(result, text);
        }

        return [.. result];
    }

    private static void AddSegment(HashSet<string> result, string? text)
    {
        var normalized = NormalizeText(text);
        if (!string.IsNullOrWhiteSpace(normalized))
            result.Add(normalized);
    }

    private static List<SearchTerm> ParseTerms(string rawQuery) =>
        rawQuery.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(SplitByCase)
                .Where(static s => s.Length > 0)
                .Select(term => new SearchTerm(NormalizeText(term), Compact(term)))
                .ToList();

    private static List<string> SplitByCase(string segment)
    {
        if (segment.Length < 3) return [segment];

        var result = new List<string>();
        var start  = 0;

        for (var i = 1; i <= segment.Length; i++)
        {
            if (i == segment.Length)
            {
                if (start < i) result.Add(segment[start..i]);

                break;
            }

            var prev = segment[i - 1];
            var curr = segment[i];

            if (char.IsLetter(prev) != char.IsLetter(curr) ||
                char.IsDigit(prev)  != char.IsDigit(curr))
            {
                result.Add(segment[start..i]);
                start = i;
                continue;
            }

            if (char.IsLower(prev) && char.IsUpper(curr))
            {
                result.Add(segment[start..i]);
                start = i;
                continue;
            }

            if (i > 1 && i < segment.Length - 1)
            {
                var twoBack = segment[i - 2];
                var next    = segment[i + 1];

                if (char.IsUpper(twoBack) && char.IsUpper(prev) && char.IsLower(curr) && char.IsLower(next))
                {
                    var splitPos = i - 1;

                    if (splitPos - start > 1)
                    {
                        result.Add(segment[start..splitPos]);
                        start = splitPos;
                    }
                }
            }
        }

        if (result.Count <= 1 || result.Any(static s => s.Length < 2))
            return [segment];

        return result;
    }

    private static int FuzzyMatch(string source, string pattern, SearchWeight weight)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(pattern)) return 0;
        if (pattern.Length > source.Length) return 0;

        var positions = new int[pattern.Length];

        var si = 0;

        for (var pi = 0; pi < pattern.Length; pi++)
        {
            while (si < source.Length && source[si] != pattern[pi])
                si++;
            if (si >= source.Length) return 0;

            positions[pi] = si;
            si++;
        }

        si = source.Length - 1;

        for (var pi = pattern.Length - 1; pi >= 0; pi--)
        {
            while (si >= 0 && source[si] != pattern[pi])
                si--;
            positions[pi] = si;
            si--;
        }

        var score = weight.BaseScore;

        for (var pi = 0; pi < pattern.Length; pi++)
        {
            score += weight.ConsecutiveBonus;
            score += PositionBonus(source, positions[pi]);

            if (pi > 0)
            {
                if (positions[pi] == positions[pi - 1] + 1)
                    score += weight.ConsecutiveBonus * 2;

                var gap = positions[pi] - positions[pi - 1] - 1;
                score -= gap * weight.GapPenalty;
            }
        }

        score += Math.Max(0, weight.BoundaryBonus - positions[0]);

        return Math.Max(score, 0);
    }

    private static int PositionBonus(string source, int pos)
    {
        if (pos == 0) return 20;

        var prev = source[pos - 1];

        if (prev is ' ' or '_' or '-' or '.' or '\n' or '/') return 16;
        if (char.IsLower(prev) && char.IsUpper(source[pos])) return 12;
        if (char.IsDigit(prev) ^ char.IsDigit(source[pos])) return 12;

        return 0;
    }

    private static int ApproximateMatch(string source, string pattern, SearchWeight weight)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(pattern)) return 0;
        if (pattern.Length < 3) return 0;

        var maxDistance  = pattern.Length <= 5 ? 1 : 2;
        var bestDistance = int.MaxValue;

        var lenDiff = source.Length - pattern.Length + 1;

        for (var start = 0; start <= lenDiff + maxDistance; start++)
        {
            var actualStart = Math.Max(0, start                    - maxDistance);
            var windowEnd   = Math.Min(source.Length, actualStart  + pattern.Length + maxDistance * 2);
            var window      = source.AsSpan(actualStart, windowEnd - actualStart);

            var distance = DamerauLevenshteinDistance(window, pattern, maxDistance);
            if (distance < 0) continue;

            bestDistance = Math.Min(bestDistance, distance);
        }

        if (bestDistance > maxDistance) return 0;

        return Math.Max(weight.ApproximateBase + pattern.Length * 4 - bestDistance * 40, 0);
    }

    private static int DamerauLevenshteinDistance(ReadOnlySpan<char> source, string pattern, int maxDistance)
    {
        var slen = source.Length;
        var plen = pattern.Length;

        var v0 = new int[plen + 1];
        var v1 = new int[plen + 1];
        var v2 = new int[plen + 1];

        for (var j = 0; j <= plen; j++)
            v0[j] = j;

        for (var i = 1; i <= slen; i++)
        {
            v2[0]  = i;
            var rowMin = v2[0];

            for (var j = 1; j <= plen; j++)
            {
                var cost = source[i - 1] == pattern[j - 1] ? 0 : 1;

                var deletion     = v1[j]     + 1;
                var insertion    = v2[j - 1] + 1;
                var substitution = v1[j - 1] + cost;

                var value = Math.Min(Math.Min(deletion, insertion), substitution);

                if (i             > 1               &&
                    j             > 1               &&
                    source[i - 1] == pattern[j - 2] &&
                    source[i - 2] == pattern[j - 1])
                    value = Math.Min(value, v0[j - 2] + 1);

                v2[j]  = value;
                rowMin = Math.Min(rowMin, value);
            }

            if (rowMin > maxDistance) return -1;

            var temp = v0;
            v0 = v1;
            v1 = v2;
            v2 = temp;
        }

        var best = int.MaxValue;
        for (var j = 0; j <= plen; j++)
            best = Math.Min(best, v1[j]);

        return best <= maxDistance ? best : -1;
    }

    private static int TryUnorderedMatch(string source, string query, SearchWeight weight)
    {
        var bestScore  = 0;
        var sourceSpan = source.AsSpan();

        for (var split = 3; split <= query.Length - 3; split++)
        {
            var part1 = query.AsSpan(0, split);
            var part2 = query[split..];

            if (QuickSubsequence(sourceSpan,         part1, out var end1) &&
                QuickSubsequence(sourceSpan[end1..], part2, out _))
            {
                var s1 = FuzzyMatch(source, part1.ToString(), weight);
                var s2 = FuzzyMatch(source, part2,            weight);
                if (s1 > 0 && s2 > 0)
                    bestScore = Math.Max(bestScore, s1 + s2);
            }

            if (QuickSubsequence(sourceSpan,         part2, out var end2) &&
                QuickSubsequence(sourceSpan[end2..], part1, out _))
            {
                var s1 = FuzzyMatch(source, part2,            weight);
                var s2 = FuzzyMatch(source, part1.ToString(), weight);
                if (s1 > 0 && s2 > 0)
                    bestScore = Math.Max(bestScore, s1 + s2);
            }
        }

        return bestScore;
    }

    private static bool QuickSubsequence(ReadOnlySpan<char> source, ReadOnlySpan<char> pattern, out int endPos)
    {
        endPos = 0;
        var si = 0;

        foreach (var t in pattern)
        {
            while (si < source.Length && source[si] != t)
                si++;
            if (si >= source.Length) return false;
            si++;
        }

        endPos = si;
        return true;
    }

    private static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var builder      = new StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var ch in text.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }

    private static string Compact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var builder = new StringBuilder(text.Length);
        AppendCompact(builder, text);
        return builder.ToString();
    }

    private static void AppendCompact(StringBuilder builder, string text)
    {
        foreach (var ch in text)
        {
            if (!char.IsLetterOrDigit(ch)) continue;
            builder.Append(char.ToLowerInvariant(ch));
        }
    }

    private static void AppendAsciiInitialism(StringBuilder builder, string text)
    {
        var takeCurrent = true;
        var previous    = '\0';

        foreach (var ch in text)
        {
            if (!char.IsLetterOrDigit(ch))
            {
                takeCurrent = true;
                previous    = '\0';
                continue;
            }

            var isBoundary =
                takeCurrent                                                                                ||
                previous == '\0'                                                                           ||
                char.IsLetter(ch) && char.IsUpper(ch) && char.IsLetter(previous) && char.IsLower(previous) ||
                char.IsDigit(ch)  && !char.IsDigit(previous)                                               ||
                !char.IsDigit(ch) && char.IsDigit(previous);

            if (isBoundary)
                builder.Append(char.ToLowerInvariant(ch));

            takeCurrent = false;
            previous    = ch;
        }
    }

    private static void AppendWordInitialism(StringBuilder builder, string text)
    {
        var takeCurrent = true;

        foreach (var ch in text)
        {
            if (!char.IsLetterOrDigit(ch))
            {
                takeCurrent = true;
                continue;
            }

            if (takeCurrent)
                builder.Append(char.ToLowerInvariant(ch));

            takeCurrent = false;
        }
    }

    private readonly record struct SearchEntry
    (
        ModuleBase  Module,
        SearchKey[] Keys
    );

    private readonly record struct SearchWeight
    (
        int BaseScore,
        int BoundaryBonus,
        int ConsecutiveBonus,
        int InitialismBase,
        int ApproximateBase,
        int GapPenalty
    );

    private readonly record struct SearchKey
    (
        string       Text,
        string       CompactText,
        string       InitialismText,
        SearchWeight Weight
    );

    private readonly record struct SearchTerm
    (
        string Normalized,
        string Compact
    );
}
