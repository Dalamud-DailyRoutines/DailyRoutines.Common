namespace DailyRoutines.Common.Module.Models;

public sealed class ModulePermission
{
    /// <summary>
    ///     需要验证才能使用, 未验证会显示为禁用状态
    /// </summary>
    public bool NeedAuth { get; init; }

    /// <summary>
    ///     在国服客户端上可用, 其他客户端会直接跳过加载
    /// </summary>
    public bool CNOnly { get; init; }

    /// <summary>
    ///     在繁中服客户端上可用, 其他客户端会直接跳过加载
    /// </summary>
    public bool TCOnly { get; init; }

    /// <summary>
    ///     在韩服客户端上可用, 其他客户端会直接跳过加载
    /// </summary>
    public bool KROnly { get; init; }

    /// <summary>
    ///     在国际服客户端上可用, 其他客户端会直接跳过加载
    /// </summary>
    public bool GLOnly { get; init; }

    /// <summary>
    ///     在全部客户端上需要测试码, 无测试码则会显示为禁用状态 <br />
    ///     若为真则会无视 <see cref="CNTester" />、<see cref="GLTester" />、<see cref="TCTester" />、
    ///     <see cref="KRTester" /> 的设置
    /// </summary>
    public bool AllTester { get; init; }

    /// <summary>
    ///     在国服客户端上需要测试码, 无测试码则会显示为禁用状态
    /// </summary>
    public bool CNTester { get; init; }

    /// <summary>
    ///     在国际服客户端上需要测试码, 无测试码则会显示为禁用状态
    /// </summary>
    public bool GLTester { get; init; }

    /// <summary>
    ///     在繁中客户端上需要测试码, 无测试码则会显示为禁用状态
    /// </summary>
    public bool TCTester { get; init; }

    /// <summary>
    ///     在韩服客户端上需要测试码, 无测试码则会显示为禁用状态
    /// </summary>
    public bool KRTester { get; init; }

    /// <summary>
    ///     在所有客户端上均默认启用 <br />
    ///     若为真则会无视 <see cref="CNDefaultEnabled" />、<see cref="GLDefaultEnabled" />、<see cref="TCDefaultEnabled" />、
    ///     <see cref="KRDefaultEnabled" /> 的设置
    /// </summary>
    public bool AllDefaultEnabled { get; init; }

    /// <summary>
    ///     在国服客户端上默认启用
    /// </summary>
    public bool CNDefaultEnabled { get; init; }

    /// <summary>
    ///     在国际服客户端上默认启用
    /// </summary>
    public bool GLDefaultEnabled { get; init; }

    /// <summary>
    ///     在繁中服客户端上默认启用
    /// </summary>
    public bool TCDefaultEnabled { get; init; }

    /// <summary>
    ///     在韩服客户端上默认启用
    /// </summary>
    public bool KRDefaultEnabled { get; init; }

    // 是否跳过加载+显示, 主要用于直接屏蔽一些仅单一客户端可用的
    public bool IsSkip()
    {
#if DEBUG
        return false;
#elif RELEASE
        var hasRestrictions = CNOnly || TCOnly || KROnly || GLOnly;
        if (!hasRestrictions) return false;

        var isAllowed = (CNOnly && GameState.IsCN) ||
                        (TCOnly && GameState.IsTC) ||
                        (KROnly && GameState.IsKR) ||
                        (GLOnly && GameState.IsGL);

        return !isAllowed;
#else
        return true;
#endif
    }

    // 是否有使用的权限, 主要判断模块权限
    public bool IsHide
    (
        bool isAuth,
        bool isTester
    )
    {
#if DEBUG
        return false;
#elif RELEASE
        var condition0 = IsSkip();
        var condition1 = NeedAuth  && !isAuth;
        var condition2 = CNTester  && GameState.IsCN && !isTester;
        var condition3 = GLTester  && GameState.IsGL && !isTester;
        var condition4 = TCTester  && GameState.IsTC && !isTester;
        var condition5 = KRTester  && GameState.IsKR && !isTester;
        var condition6 = AllTester && !isTester;

        return condition0 || condition1 || condition6 || condition2 || condition3 || condition4 || condition5;
#else
        return true;
#endif
    }

    // 是否默认启用
    public bool IsDefaultEnabled()
    {
        if (AllDefaultEnabled)
            return true;

        var isDefaultEnabled = false;

        if (GameState.IsCN && CNDefaultEnabled)
            isDefaultEnabled = true;

        if (GameState.IsGL && GLDefaultEnabled)
            isDefaultEnabled = true;

        if (GameState.IsTC && TCDefaultEnabled)
            isDefaultEnabled = true;

        if (GameState.IsKR && KRDefaultEnabled)
            isDefaultEnabled = true;

        return isDefaultEnabled;
    }
}
