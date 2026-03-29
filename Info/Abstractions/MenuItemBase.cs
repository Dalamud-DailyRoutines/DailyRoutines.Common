using System;
using System.Text;
using DailyRoutines.Common.Extensions;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using MenuItem = Dalamud.Game.Gui.ContextMenu.MenuItem;

namespace DailyRoutines.Common.Info.Abstractions;

public abstract class MenuItemBase
{
    private Lazy<MenuItem> MenuItem => new
    (() => new()
        {
            Name             = FinalName.Value,
            OnClicked        = OnClicked,
            PrefixColor      = PrefixColor,
            Prefix           = Prefix,
            UseDefaultPrefix = false,
            IsSubmenu        = IsSubmenu,
            Priority         = OrderGenerator.GetOrder(Name, Identifier)
        }
    );

    private Lazy<SeString> FinalName => new
    (() =>
        {
            if (!WithDRPrefix)
                Prefix = SeIconChar.BoxedStar;
            return WithDRPrefix ? Name.WithRPrefix() : Name;
        }
    );

    public void Click(IMenuItemClickedArgs? args) => OnClicked(args);

    public MenuItem Get() => MenuItem.Value;

    private static class OrderGenerator
    {
        private const int GROUP_RANGE      = 1000;
        private const int TOTAL_RANGE      = 1000001;
        private const int NUMBER_OF_GROUPS = TOTAL_RANGE / GROUP_RANGE;

        public static int GetOrder(string name, string identifier)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(identifier))
                return 0;

            var identifierHash = GetStableHash(identifier);
            var groupIndex     = (int)(identifierHash % NUMBER_OF_GROUPS);
            var baseOrder      = groupIndex * GROUP_RANGE;
            var nameHash       = GetStableHash(name);
            var localOffset    = (int)(nameHash % GROUP_RANGE);
            var finalOrder     = baseOrder + localOffset;

            return Math.Min(finalOrder, TOTAL_RANGE);
        }

        private static uint GetStableHash(string text)
        {
            const uint FNV_PRIME        = 16777619;
            const uint FNV_OFFSET_BASIS = 2166136261;

            var hash = FNV_OFFSET_BASIS;
            var data = Encoding.UTF8.GetBytes(text);

            foreach (var b in data)
            {
                unchecked
                {
                    hash ^= b;
                    hash *= FNV_PRIME;
                }
            }

            return hash;
        }
    }

    #region 继承

    public abstract string Name       { get; protected set; }
    public abstract string Identifier { get; protected set; }

    protected virtual ushort     PrefixColor  { get; set; } = 34;
    protected virtual bool       WithDRPrefix { get; set; }
    protected virtual SeIconChar Prefix       { get; set; } = SeIconChar.BoxedLetterD;
    protected virtual bool       IsSubmenu    { get; set; }

    protected abstract void OnClicked(IMenuItemClickedArgs args);

    public virtual bool IsDisplay(IMenuOpenedArgs args) => true;

    #endregion
}
