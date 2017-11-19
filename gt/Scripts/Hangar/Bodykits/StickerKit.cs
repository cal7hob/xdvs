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
        if (MenuController.Instance == null || MenuController.Instance.isBattleEntering) 
        {
            if (decal != null && decal.id == id)
            {
                gameObject.SetActive(true);
            }
            else 
            {
                Destroy(gameObject);
            }
        }
        else 
        {
            gameObject.SetActive(decal != null && decal.id == id);
        }
    }
}
