using DailyRoutines.Common.Interface.ImGuiDR;

namespace DailyRoutines.Common.Extensions;

public static class ImRaiiExtension
{
    extension(ImRaii)
    {
        public static Heading1 Heading1(string text) => 
            new(text);
        
        public static Heading2 Heading2(string text) => 
            new(text);
    }
}
