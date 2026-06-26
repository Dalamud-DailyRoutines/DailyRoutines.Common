using DailyRoutines.Common.Info;

namespace DailyRoutines.Common.Interface.ImGuiDR;

public ref struct Heading1 : IDisposable
{
    public bool Alive { get; private set; }

    public Heading1(string text)
    {
        Alive = true;
        
        ImGui.TextColored(Colors.Heading1, text);
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
