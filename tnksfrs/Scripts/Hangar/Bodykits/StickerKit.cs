using UnityEngine;

public class StickerKit : MonoBehaviour
{
    public int id;

    #if UNITY_EDITOR

    [Header("Парсинг ID")]
    public string regexPattern = @"_(\d+)$";

    #endif
}
