namespace DailyRoutines.Common.Module.Search.Models;

public sealed class ModuleSearchOptions
{
    public string?              CategoryText  { get; init; }
    public IEnumerable<string>? ExtraSegments { get; init; }
}
