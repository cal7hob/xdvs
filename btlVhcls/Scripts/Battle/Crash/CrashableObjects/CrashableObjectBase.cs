using System;
using UnityEngine;

public abstract class CrashableObjectBase : MonoBehaviour
{
    protected const float SOUND_VOLUME_RATIO = 0.5f;

    private bool isCrashed;

    protected abstract void CrashItself(Collider collider);

    protected abstract void RestoreItself();

    public void Crash(Collider collider)
    {
        if (isCrashed)
            return;

        isCrashed = true;

        CrashItself(collider);
    }

    /// <summary>
    /// Нигде не используется. Возможно, этот метод вообще не нужен. И реализация не везде прописана.
    /// </summary>
    [Obsolete]
    public void Restore()
    {
        isCrashed = false;
        RestoreItself();
    }
}
