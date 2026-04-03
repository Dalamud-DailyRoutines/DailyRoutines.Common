namespace DailyRoutines.Common.Module.Search.Models;

internal readonly record struct SearchTerm
(
    string Normalized,
    string Compact
);
