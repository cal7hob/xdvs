using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class SpriteGroup
{
    private bool active;
    private bool multiSprite;
    
    public SpriteGroup(Transform parentTransform)
    {
        if (!parentTransform)
        {
            throw new Exception("Null as parentTransform SpriteGroup constructor");
        }
        
      
        GO = parentTransform.gameObject;
        active = true;
    }

    public GameObject GO { get; private set; }
}
