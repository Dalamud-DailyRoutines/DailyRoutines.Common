using System.Text;
using DailyRoutines.Common.Module.Abstractions;
using DailyRoutines.Common.Module.Search.Models;
using TinyPinyin;

namespace DailyRoutines.Common.Module.Search;

public static class ModuleSearcher
{
    public static ModuleSearchDocument CreateDocument(ModuleBase module, ModuleSearchOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(module);

        var info           = module.Info;
        var moduleType     = module.GetType();
        var categoryKey    = info.Category.ToString();
        var categoryText   = options?.CategoryText;
        var authorText     = string.Join(" / ", info.Author);
        var supportText    = string.Join(" / ", info.SupportUrls);
        var previewText    = string.Join(" / ", info.PreviewImageURL);
        var precedingText  = string.Join(" / ", module.PrecedingModules?.Select(static x => x.ModuleName) ?? []);
        var recommendText  = string.Join(" / ", module.RecommendModules?.Select(static x => x.ModuleName) ?? []);
        var conflictText   = string.Join(" / ", module.ConflictModules?.Select(static x => x.ModuleName)  ?? []);
        var identifierText = string.IsNullOrWhiteSpace(module.ModuleIdentifier) ? module.ModuleName : module.ModuleIdentifier;

        var primarySegments = new[]
        {
            info.Title,
            module.ModuleName,
            moduleType.Name,
            moduleType.FullName,
            identifierText,
            module.ModuleGUID,
            categoryKey,
            categoryText,
            authorText,
            info.Description,
            string.Join(" / ", info.ModulesPrerequisite),
            string.Join(" / ", info.ModulesRecommend),
            string.Join(" / ", info.ModulesConflict),
            precedingText,
            recommendText,
            conflictText,
            info.ReportURL,
            supportText,
            previewText
        };

        return new
        (
            module,
            JoinSegments(primarySegments, options?.ExtraSegments),
            CreateField([info.Title]),
            CreateField([module.ModuleName, moduleType.Name, moduleType.FullName]),
            CreateField([identifierText, module.ModuleGUID]),
            CreateField([categoryKey, categoryText]),
            CreateField
            (
                [
                    authorText,
                    info.Description,
                    string.Join(" / ", info.ModulesPrerequisite),
                    string.Join(" / ", info.ModulesRecommend),
                    string.Join(" / ", info.ModulesConflict),
                    precedingText,
                    recommendText,
                    conflictText,
                    info.ReportURL,
                    supportText,
                    previewText
                ],
                options?.ExtraSegments
            )
        );
    }

    public static ModuleSearchQuery ParseQuery(string query) =>
        new(query, ParseTerms(query));

    public static bool TryScore(ModuleSearchDocument document, string query, out int score)
    {
        var parsedQuery = ParseQuery(query);
        return TryScore(document, parsedQuery, out score);
    }

    public static bool TryScore(ModuleSearchDocument document, ModuleSearchQuery query, out int score)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(query);

        if (query.IsEmpty)
        {
            score = 0;
            return false;
        }

        score = 0;

        foreach (var term in query.Terms)
        {
            if (!TryScoreTerm(document, term, out var termScore))
            {
                score = 0;
                return false;
            }

            score += termScore;
        }

        score += query.Terms.Count * 24;
        return true;
    }

    private static bool TryScoreTerm(ModuleSearchDocument document, SearchTerm term, out int score)
    {
        score = 0;

        score = Math.Max(score, ScoreField(document.Title,      term, 2_400, 2_000, 1_880, 1_650, 1_520));
        score = Math.Max(score, ScoreField(document.Name,       term, 2_260, 1_920, 1_760, 1_560, 1_420));
        score = Math.Max(score, ScoreField(document.Identifier, term, 2_120, 1_820, 1_680, 1_480, 1_360));
        score = Math.Max(score, ScoreField(document.Category,   term, 1_680, 1_420, 1_320, 1_180, 1_060));
        score = Math.Max(score, ScoreField(document.Meta,       term, 1_320, 1_120, 1_020, 900,   820));

        return score > 0;
    }

    private static int ScoreField(SearchField field, SearchTerm term, int exactBase, int containsBase, int compactBase, int subsequenceBase, int initialismBase)
    {
        var bestScore = 0;

        if (!string.IsNullOrEmpty(field.Text))
        {
            if (field.Text.Equals(term.Normalized, StringComparison.Ordinal))
                bestScore = Math.Max(bestScore, exactBase + term.Normalized.Length * 4);

            var startsWithScore = ScorePrefix(field.Text, term.Normalized, containsBase + 180);
            if (startsWithScore > 0)
                bestScore = Math.Max(bestScore, startsWithScore);

            var containsScore = ScoreContains(field.Text, term.Normalized, containsBase);
            if (containsScore > 0)
                bestScore = Math.Max(bestScore, containsScore);
        }

        if (!string.IsNullOrEmpty(term.Compact))
        {
            var compactScore = ScoreContains(field.CompactText, term.Compact, compactBase);
            if (compactScore > 0)
                bestScore = Math.Max(bestScore, compactScore);

            var initialismScore = ScoreContains(field.InitialismText, term.Compact, initialismBase);
            if (initialismScore > 0)
                bestScore = Math.Max(bestScore, initialismScore);

            var subsequenceScore = ScoreSubsequence(field.CompactText, term.Compact, subsequenceBase);
            if (subsequenceScore > 0)
                bestScore = Math.Max(bestScore, subsequenceScore);
        }

        return bestScore;
    }

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

    private static List<SearchTerm> ParseTerms(string query)
    {
        var normalized = NormalizeText(query);
        if (string.IsNullOrWhiteSpace(normalized)) return [];

        return normalized.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                         .Select(static term => new SearchTerm(term, Compact(term)))
                         .ToList();
    }

    private static SearchField CreateField(IEnumerable<string?> primarySegments, IEnumerable<string?>? extraSegments = null)
    {
        var segments = CollectSegments(primarySegments, extraSegments);
        if (segments.Count == 0) return SearchField.Empty;

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
                !segment.Equals(pinyinCompact, StringComparison.OrdinalIgnoreCase)) AppendCompact(compactBuilder, pinyinCompact);

            var pinyinWords = PinyinHelper.GetPinyin(segment);
            if (!string.IsNullOrWhiteSpace(pinyinWords))
                AppendWordInitialism(initialismBuilder, pinyinWords);
        }

        return new(textBuilder.ToString(), compactBuilder.ToString(), initialismBuilder.ToString());
    }

    private static string JoinSegments(IEnumerable<string?> primarySegments, IEnumerable<string?>? extraSegments = null)
    {
        var segments = CollectSegments(primarySegments, extraSegments);
        return segments.Count == 0 ? string.Empty : string.Join('\n', segments);
    }

    private static List<string> CollectSegments(IEnumerable<string?> primarySegments, IEnumerable<string?>? extraSegments = null)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);

        foreach (var text in primarySegments)
        {
            var normalized = NormalizeText(text);
            if (!string.IsNullOrWhiteSpace(normalized))
                result.Add(normalized);
        }

        if (extraSegments == null)
            return result.ToList();

        foreach (var text in extraSegments)
        {
            var normalized = NormalizeText(text);
            if (!string.IsNullOrWhiteSpace(normalized))
                result.Add(normalized);
        }

        return result.ToList();
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
}
