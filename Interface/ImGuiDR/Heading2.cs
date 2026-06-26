using DailyRoutines.Common.Info;

namespace DailyRoutines.Common.Interface.ImGuiDR;

public ref struct Heading2 : IDisposable
{
    public bool Alive { get; private set; }

    public Heading2(string text)
    {
        Alive = true;
        
        ImGui.TextColored(Colors.Heading2, text);
        ImGui.Indent();
    }

    public void Dispose()
    {
        if (!Alive)
            return;

        ImGui.Unindent();
        Alive = false;
    }
}
