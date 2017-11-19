using UnityEngine;

public class StickerKit : MonoBehaviour
{
    public int id;

    #if UNITY_EDITOR
    [Header("Парсинг ID")]
    public string regexPattern = @"_(\d+)$";
    #endif

    public virtual void TryActivate(Decal decal)
    {
        gameObject.SetActive(decal != null && decal.id == id);
    }

    public virtual void TryActivate(int stickerId)
    {
        gameObject.SetActive(stickerId != 0 && stickerId == id);
    }
}
