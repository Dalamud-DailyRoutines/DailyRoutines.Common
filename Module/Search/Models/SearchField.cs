namespace DailyRoutines.Common.Module.Search.Models;

internal readonly record struct SearchField
(
    string Text,
    string CompactText,
    string InitialismText
)
{
    public static SearchField Empty { get; } = new(string.Empty, string.Empty, string.Empty);
}
