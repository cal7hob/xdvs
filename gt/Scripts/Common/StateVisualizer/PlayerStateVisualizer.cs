using System;

[Flags]
public enum PlayerVisualState
{
    None = 1 << 0,    // 1
    Self = 1 << 1,    // 2
    Online = 1 << 2,  // 4
    Offline = 1 << 3, // 8
    Vip = 1 << 4,     // 16
}

// Unity Serialization, my ass
[Serializable]
public class VisualStateOfPlayer : VisualState<PlayerVisualState> { }

public class PlayerStateVisualizer : StateVisualizer
{
    /// <summary>
    /// Current visual state
    /// </summary>
    public PlayerVisualState playerState;

    /// <summary>
    /// Collection of GameObjects and their states to be triggered
    /// </summary>
    public VisualStateOfPlayer[] States;

    public PlayerStateVisualizer()
    {
        States = new VisualStateOfPlayer[Enum.GetValues(typeof(PlayerVisualState)).Length];
    }

    public void SetState(PlayerVisualState state)
    {
        playerState = state;
        base.SetState(state, States);
    }
}
