using System;

/// <summary>
/// Флаги режимов показа рекламы.
/// </summary>
[Flags]
public enum AdsShowingMode
{
    Nowhere         = 0,
    BeforeBattle    = 1 << 0,
    AfterBattle     = 1 << 1,
    OnQuit          = 1 << 2,
    Everywhere      = BeforeBattle | AfterBattle | OnQuit
}