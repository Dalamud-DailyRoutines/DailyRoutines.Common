using DailyRoutines.Common.Info;
using Dalamud.Game.Text.SeStringHandling;

namespace DailyRoutines.Common.Extensions;

public static class StringExtension
{
    extension(string text)
    {
        public SeString WithDRPrefix() => 
            new SeStringBuilder()
                .Append(Assets.BoxedLettersDR)
                .AddText($" {text}")
                .Build();
    
        public SeString WithDPrefix() => 
            new SeStringBuilder()
                .Append(Assets.BoxedLetterD)
                .Append($" {text}")
                .Build();
        
        public SeString WithRPrefix() => 
            new SeStringBuilder()
                .Append(Assets.BoxedLetterR)
                .Append($" {text}")
                .Build();
    }
}
