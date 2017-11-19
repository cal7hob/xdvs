using System;

[Flags]
public enum ClanVisualState
{
    None = 1 << 0, // 1
    Self = 1 << 1, // 2
}

// Unity Serialization, my ass
[Serializable]
public class VisualStateOfClan : VisualState<ClanVisualState> { }

public class ClanStateVisualizer : StateVisualizer
{
    /// <summary>
    /// Current visual state
    /// </summary>
    public ClanVisualState clanState;

    /// <summary>
    /// Collection of GameObjects and their states to be triggered
    /// </summary>
    public VisualStateOfClan[] States;

    public ClanStateVisualizer()
    {
        States = new VisualStateOfClan[Enum.GetValues(typeof(ClanVisualState)).Length];
    }

    public void SetState(ClanVisualState state)
    {
        clanState = state;
        base.SetState(state, States);
    }
}
