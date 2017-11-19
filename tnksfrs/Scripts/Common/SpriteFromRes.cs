using UnityEngine;
using System.Collections;

public class SpriteFromRes : MonoBehaviour 
{
	[SerializeField]private string texName = "";
	private static Texture2D whiteSquare;//маленькая текстура на которую будут заменяться загружаемые из ресурсов текстуры(для выгрузки их из памяти)
    private bool isInited = false;

    private void OnEnable()
	{
        SetTexture(texName);
    }

    private void Init()
    {
        if (!isInited)
        {

            if (whiteSquare == null)
                whiteSquare = (Texture2D)Resources.Load("Common/white");
            isInited = true;
        }
    }

	private void OnDisable()
	{
		Resources.UnloadUnusedAssets();
	}

    public void SetTexture(string _texName)
    {
        Init();
        texName = _texName;
        /*string path = string.Format("{0}/Textures/{1}/{2}", GameData.CurInterface, tk2dSystem.CurrentPlatform, texName);
        Texture2D texToReplace = (Texture2D)Resources.Load(path);
        //DT.LogWarning("SpriteFromRes. Set texture {0}", "Assets/Resources/" + path);
        if (texToReplace == null)
        {
            DT.LogError("SpriteFromRes. Cant find texture {0}", "Assets/Resources/" + path);
            return;
        }*/
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
    }
}
