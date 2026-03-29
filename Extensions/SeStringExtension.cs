using DailyRoutines.Common.Info;
using Dalamud.Game.Text.SeStringHandling;

namespace DailyRoutines.Common.Extensions;

public static class SeStringExtension
{
    extension(SeString text)
    {
        public SeString WithDRPrefix() => 
            new SeStringBuilder()
                .Append(Assets.BoxedLettersDR)
                .AddText(" ")
                .Append(text)
                .Build();
    
        public SeString WithDPrefix() => 
            new SeStringBuilder()
                .Append(Assets.BoxedLetterD)
                .Append(" ")
                .Append(text)
                .Build();
        
        public SeString WithRPrefix() => 
            new SeStringBuilder()
                .Append(Assets.BoxedLetterR)
                .Append(" ")
                .Append(text)
                .Build();
    }
}
