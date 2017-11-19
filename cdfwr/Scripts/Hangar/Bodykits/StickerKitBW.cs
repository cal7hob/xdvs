using System;
using UnityEngine;

public class StickerKitBW : StickerKit
{
    [Serializable]
    public class StickerOffset
    {
        public int id;
        public Vector2 value;
    }

    public StickerOffset[] offsets;

    private new MeshRenderer renderer;

    void Awake()
    {
        renderer = GetComponent<MeshRenderer>();
    }

    public override void TryActivate(Decal decal)
    {
        if (decal == null)
        {
            gameObject.SetActive(false);
            return;
        }

        foreach (StickerOffset offset in offsets)
        {
            if (offset.id == decal.id)
            {
                gameObject.SetActive(true);
                renderer.material.SetTextureOffset("_MainTex", offset.value);
                return;
            }
        }
        gameObject.SetActive(false);
        return;
    }
}
