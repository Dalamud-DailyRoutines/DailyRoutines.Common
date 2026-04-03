namespace DailyRoutines.Common.Module.Search.Models;

public sealed class ModuleSearchQuery
{
    internal ModuleSearchQuery(string rawText, IReadOnlyList<SearchTerm> terms)
    {
        RawText = rawText;
        Terms   = terms;
    }

    public string RawText { get; }
    public bool   IsEmpty => Terms.Count == 0;

    internal IReadOnlyList<SearchTerm> Terms { get; }
}
