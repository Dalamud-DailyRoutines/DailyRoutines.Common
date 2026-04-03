using DailyRoutines.Common.Module.Abstractions;

namespace DailyRoutines.Common.Module.Search.Models;

public sealed class ModuleSearchDocument
{
    internal ModuleSearchDocument
        (ModuleBase module, string searchText, SearchField title, SearchField name, SearchField identifier, SearchField category, SearchField meta)
    {
        Module     = module;
        SearchText = searchText;
        Title      = title;
        Name       = name;
        Identifier = identifier;
        Category   = category;
        Meta       = meta;
    }

    public ModuleBase Module     { get; }
    public string     SearchText { get; }

    internal SearchField Title      { get; }
    internal SearchField Name       { get; }
    internal SearchField Identifier { get; }
    internal SearchField Category   { get; }
    internal SearchField Meta       { get; }
}
