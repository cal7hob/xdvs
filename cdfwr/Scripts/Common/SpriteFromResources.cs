using UnityEngine;
using System.Collections;

public class SpriteFromResources : MonoBehaviour 
{
    [SerializeField]private string texName = "";
    [SerializeField]private tk2dSpriteFromTexture sprFromTex;
    private static Texture2D whiteSquare;//маленькая текстура на которую будут заменяться загружаемые из ресурсов текстуры(для выгрузки их из памяти)
    [SerializeField]private tk2dCameraAnchor controlledAnchor;
    private bool isInited = false;

    public tk2dBaseSprite Sprite { get; private set; }

    private void OnEnable()
    {
        SetTexture(texName);
    }

    private void Init()
    {
        if (!isInited)
        {
            if (controlledAnchor && controlledAnchor.AnchorCamera == null && GameData.CurSceneGuiCamera != null)
                controlledAnchor.AnchorCamera = GameData.CurSceneGuiCamera;

            if (whiteSquare == null)
                whiteSquare = (Texture2D)Resources.Load("Common/white");
            if (Sprite == null)
                Sprite = sprFromTex.GetComponent<tk2dBaseSprite>();
            isInited = true;
        }
    }

    private void OnDisable()
    {
        sprFromTex.texture = whiteSquare;
        sprFromTex.ForceBuild();
        Resources.UnloadUnusedAssets();
    }

    public void SetTexture(string _texName)
    {
        Init();
        texName = _texName;
        Texture2D texToReplace = (Texture2D)Resources.Load(texName);
        //DT.LogWarning("SpriteFromRes. Set texture {0}", "Assets/Resources/" + path);
        if (texToReplace == null)
        {
            DT.LogError("SpriteFromRes. Cant find texture {0}", "Assets/Resources/" + texName);
            return;
        }
        sprFromTex.texture = texToReplace;
        sprFromTex.ForceBuild();
        sprFromTex.gameObject.SetActive(true);
    }

    public void SetTexture(string _texName, float x, float y)
    {
        Init();
        SetTexture(_texName);
        SetTextureDimensions(x,y);
    }

    public void SetTextureDimensions(float x, float y)
    {
        Init();
        if (Sprite is tk2dSlicedSprite)
            ((tk2dSlicedSprite)Sprite).dimensions = new Vector2(x,y);
    }
}
