using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LocalizationFontAgent : MonoBehaviour
{
    private tk2dTextMesh textMesh;

    void Awake()
    {
        textMesh = GetComponent<tk2dTextMesh>();
        Localizer.AddFontAgent(this);
    }

    void OnDestroy()
    {
        Localizer.RemoveFontAgent(this);
    }
    
    
    public void SetFontData(tk2dFontData font)
    {
        if (font != null)
            textMesh.font = font;
    }
}
