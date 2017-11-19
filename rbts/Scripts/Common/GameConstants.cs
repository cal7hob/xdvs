using UnityEngine;

public static class GameConstants
{
    public const string PHOTON_ROOM_VERSION = "v8.1";

    #region Настройки ботов
    public const float SIDEWAYS_LOOK_INTERVAL = 0.15f;
    public const float FORWARD_LOOK_INTERVAL = 0.1f;
    #endregion

    private static WaitForFixedUpdate fixedUpdateWaiter = null;
    public static WaitForFixedUpdate FixedUpdateWaiter
    {
        get
        {
            if (fixedUpdateWaiter == null)
                fixedUpdateWaiter = new WaitForFixedUpdate();
            return fixedUpdateWaiter;
        }
    }
}
