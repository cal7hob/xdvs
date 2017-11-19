using System;
using UnityEngine;

[Obsolete("Используй CrashableObjectReplace")]
public class CrashObject : MonoBehaviour // TODO: удалить.
{
    public GameObject defaultVersion;
    public GameObject crashVersion;

    public void Crash()
    {
        crashVersion.SetActive(true);
        defaultVersion.SetActive(false);
    }
}
