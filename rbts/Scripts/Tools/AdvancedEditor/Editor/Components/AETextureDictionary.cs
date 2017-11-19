using UnityEngine;

public class AETextureDictionary : AEDictionary<string, Texture>
{
    public override Texture this[string key]
    {
        get
        {
            int id = keys.IndexOf(key);
            Texture texture;
            if (id == -1)
            {
                keys.Add(key);
                values.Add(texture = AEEditorTools.LoadTexture(key));
                return texture;
            }
            else
            {
                if ((texture = values[id]) == null) Remove(key);
                return texture;
            }
        }
        set
        {
            base[key] = value;
        }
    }
}
