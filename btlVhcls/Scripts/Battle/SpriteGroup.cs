using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class SpriteGroup
{
    private tk2dSprite[] sprites;
    private bool active;
    private bool multiSprite;
    
    public SpriteGroup(Transform parentTransform)
    {
        if (!parentTransform)
        {
            throw new Exception("Null as parentTransform SpriteGroup constructor");
        }
        
        if ((sprites = parentTransform.GetComponentsInChildren<tk2dSprite>()) == null || sprites.Length == 0)
        {
            Debug.LogError("There are no sprites for SpriteGroup", parentTransform.gameObject);
            return;
        }

        multiSprite = sprites.Length > 1;
        GO = parentTransform.gameObject;
        active = true;
    }

    public GameObject GO { get; private set; }
    
    public Vector3 SpriteScale
    {
        get { return sprites[0].scale; }
        set { SetScale(value); }
    }

    public Color SpriteColor
    {
        get { return sprites[0].color; }
        set { sprites[0].color = value; }
    }

    public void SetSprite(string spriteName)
    {
        if (!active || multiSprite)
            return;

        sprites[0].SetSprite(spriteName);
    }

    public string CurrentSpriteName
    {
        get { return multiSprite ? "" : sprites[0].CurrentSprite.name; }
    }


    private void SetScale(Vector3 newScale)
    {
        if (!active)
            return;

        for (int i = 0; i < sprites.Length; i++)
            sprites[i].scale = newScale;
    }
}
