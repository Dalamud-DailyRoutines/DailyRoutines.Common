using System.Text;
using DailyRoutines.Common.Module.Abstractions;
using TinyPinyin;

namespace DailyRoutines.Common.Module;

public sealed class ModuleSearcher
{
    private static readonly SearchWeight TitleWeight      = new(2_400, 2_000, 1_880, 1_650, 1_520, 1_760);
    private static readonly SearchWeight NameWeight       = new(2_260, 1_920, 1_760, 1_560, 1_420, 1_620);
    private static readonly SearchWeight IdentifierWeight = new(2_120, 1_820, 1_680, 1_480, 1_360, 1_520);
    private static readonly SearchWeight CategoryWeight   = new(1_680, 1_420, 1_320, 1_180, 1_060, 1_180);
    private static readonly SearchWeight MetaWeight       = new(1_320, 1_120, 1_020, 900, 820, 980);

    private readonly SearchEntry[] entries;

    private string?      cachedQuery;
    private ModuleBase[] cachedResult = [];

    public ModuleSearcher
    (
        IEnumerable<ModuleBase> modules,
        Func<ModuleBase, string?>? categoryTextSelector = null,
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
                    categoryTextSelector?.Invoke(module),
                    extraSegmentsSelector?.Invoke(module)
                )
            );
        }

        entries = [.. builtEntries];
    }

    public ModuleBase[] Search(string query)
    {
        var normalizedQuery = NormalizeText(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            cachedQuery  = normalizedQuery;
            cachedResult = [];
            return cachedResult;
        }

        if (string.Equals(cachedQuery, normalizedQuery, StringComparison.Ordinal))
            return cachedResult;

        var terms = ParseTerms(normalizedQuery);
        List<(ModuleBase Module, int Score)> matches = [];

        foreach (var entry in entries)
        {
            if (!TryScore(entry, terms, out var score)) continue;
            matches.Add((entry.Module, score));
        }

        matches.Sort
        (
            static (left, right) =>
            {
                var scoreComparison = right.Score.CompareTo(left.Score);
                return scoreComparison != 0
                           ? scoreComparison
                           : string.Compare(left.Module.Info.Title, right.Module.Info.Title, StringComparison.OrdinalIgnoreCase);
            }
        );

        cachedQuery  = normalizedQuery;
        cachedResult = [.. matches.Select(static x => x.Module)];
        return cachedResult;
    }

    private static SearchEntry CreateEntry(ModuleBase module, string? categoryText, IEnumerable<string?>? extraSegments)
    {
        var info           = module.Info;
        var moduleType     = module.GetType();
        var identifierText = string.IsNullOrWhiteSpace(module.ModuleIdentifier) ? module.ModuleName : module.ModuleIdentifier;
        var categoryKey    = info.Category.ToString();

        return new
        (
            module,
            [
                CreateKey([info.Title], TitleWeight),
                CreateKey([module.ModuleName, moduleType.Name, moduleType.FullName], NameWeight),
                CreateKey([identifierText, module.ModuleGUID], IdentifierWeight),
                CreateKey([categoryKey, categoryText], CategoryWeight),
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
                        string.Join(" / ", module.ConflictModules?.Select(static x => x.ModuleName) ?? []),
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
        var bestScore = 0;

        if (!string.IsNullOrEmpty(key.Text))
        {
            if (key.Text.Equals(term.Normalized, StringComparison.Ordinal))
                bestScore = Math.Max(bestScore, key.Weight.ExactBase + term.Normalized.Length * 4);

            var startsWithScore = ScorePrefix(key.Text, term.Normalized, key.Weight.ContainsBase + 180);
            if (startsWithScore > 0)
                bestScore = Math.Max(bestScore, startsWithScore);

            var containsScore = ScoreContains(key.Text, term.Normalized, key.Weight.ContainsBase);
            if (containsScore > 0)
                bestScore = Math.Max(bestScore, containsScore);
        }

        if (!string.IsNullOrEmpty(term.Compact))
        {
            var compactScore = ScoreContains(key.CompactText, term.Compact, key.Weight.CompactBase);
            if (compactScore > 0)
                bestScore = Math.Max(bestScore, compactScore);

            var initialismScore = ScoreContains(key.InitialismText, term.Compact, key.Weight.InitialismBase);
            if (initialismScore > 0)
                bestScore = Math.Max(bestScore, initialismScore);

            var subsequenceScore = ScoreSubsequence(key.CompactText, term.Compact, key.Weight.SubsequenceBase);
            if (subsequenceScore > 0)
                bestScore = Math.Max(bestScore, subsequenceScore);

            var fuzzyScore = ScoreApproximate(key.CompactText, term.Compact, key.Weight.FuzzyBase);
            if (fuzzyScore > 0)
                bestScore = Math.Max(bestScore, fuzzyScore);
        }

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

    private static List<SearchTerm> ParseTerms(string normalizedQuery) =>
        normalizedQuery.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                       .Select(static term => new SearchTerm(term, Compact(term)))
                       .ToList();

    private static int ScorePrefix(string source, string pattern, int baseScore)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(pattern)) return 0;
        if (!source.StartsWith(pattern, StringComparison.Ordinal)) return 0;
        return baseScore + pattern.Length * 4 - Math.Min(source.Length - pattern.Length, 96);
    }

    private static int ScoreContains(string source, string pattern, int baseScore)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(pattern)) return 0;

        var index = source.IndexOf(pattern, StringComparison.Ordinal);
        if (index < 0) return 0;

        return baseScore + pattern.Length * 3 - Math.Min(index, 96);
    }

    private static int ScoreSubsequence(string source, string pattern, int baseScore)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(pattern)) return 0;
        if (pattern.Length > source.Length) return 0;

        var patternIndex = 0;
        var gapPenalty   = 0;
        var lastMatch    = -1;

        for (var i = 0; i < source.Length && patternIndex < pattern.Length; i++)
        {
            if (source[i] != pattern[patternIndex]) continue;

            if (lastMatch >= 0)
                gapPenalty += i - lastMatch - 1;

            lastMatch = i;
            patternIndex++;
        }

        if (patternIndex != pattern.Length) return 0;

        return Math.Max(baseScore + pattern.Length * 2 - gapPenalty, 0);
    }

    private static int ScoreApproximate(string source, string pattern, int baseScore)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(pattern)) return 0;
        if (pattern.Length < 3) return 0;

        var maxDistance = GetMaxDistance(pattern.Length);
        if (maxDistance == 0) return 0;

        var distance = ApproximateSubstringDistance(source, pattern, maxDistance);
        if (distance < 0) return 0;

        return Math.Max(baseScore + pattern.Length * 3 - distance * 120, 0);
    }

    private static int GetMaxDistance(int patternLength) =>
        patternLength switch
        {
            <= 2 => 0,
            <= 4 => 1,
            <= 8 => 2,
            _    => Math.Min(3, patternLength / 4)
        };

    private static int ApproximateSubstringDistance(string source, string pattern, int maxDistance)
    {
        if (pattern.Length == 0) return 0;
        if (source.Length == 0) return pattern.Length <= maxDistance ? pattern.Length : -1;

        var previous = new int[source.Length + 1];
        var current  = new int[source.Length + 1];

        for (var j = 0; j <= source.Length; j++)
            previous[j] = 0;

        for (var i = 1; i <= pattern.Length; i++)
        {
            current[0] = i;
            var rowMin = current[0];

            for (var j = 1; j <= source.Length; j++)
            {
                var substitutionCost = pattern[i - 1] == source[j - 1] ? 0 : 1;
                var deletion         = previous[j] + 1;
                var insertion        = current[j - 1] + 1;
                var substitution     = previous[j - 1] + substitutionCost;

                var value = Math.Min(Math.Min(deletion, insertion), substitution);

                current[j] = value;
                rowMin     = Math.Min(rowMin, value);
            }

            if (rowMin > maxDistance)
                return -1;

            (previous, current) = (current, previous);
        }

        var bestDistance = int.MaxValue;
        for (var j = 0; j <= source.Length; j++)
            bestDistance = Math.Min(bestDistance, previous[j]);

        return bestDistance <= maxDistance ? bestDistance : -1;
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
                takeCurrent ||
                previous == '\0' ||
                (char.IsLetter(ch) && char.IsUpper(ch) && char.IsLetter(previous) && char.IsLower(previous)) ||
                (char.IsDigit(ch) && !char.IsDigit(previous)) ||
                (!char.IsDigit(ch) && char.IsDigit(previous));

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

    private readonly record struct SearchEntry(ModuleBase Module, SearchKey[] Keys);

    private readonly record struct SearchWeight
    (
        int ExactBase,
        int ContainsBase,
        int CompactBase,
        int SubsequenceBase,
        int InitialismBase,
        int FuzzyBase
    );

    private readonly record struct SearchKey
    (
        string Text,
        string CompactText,
        string InitialismText,
        SearchWeight Weight
    );

    private readonly record struct SearchTerm(string Normalized, string Compact);
}
