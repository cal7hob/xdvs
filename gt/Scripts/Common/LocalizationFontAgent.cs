using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LocalizationFontAgent : MonoBehaviour
{
    private static Dictionary<string, tk2dFontData> fontData = new Dictionary<string, tk2dFontData>();
    
    private tk2dTextMesh textMesh;
    private Localizer.LocalizationLanguage fontLoaded = Localizer.LocalizationLanguage.Undefined;

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
