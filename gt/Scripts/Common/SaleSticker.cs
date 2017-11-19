using UnityEngine;
using System.Collections;
using System;

public class SaleSticker : MonoBehaviour
{

    public GameObject sprSaleSticker;
    public tk2dTextMesh lblSaleText;
    private const float textSize = 0.6f;
    [Header("Вместо ентера писать \n")]
    public string formatString = "";

    public string Text
    {
        set
        {
            if(lblSaleText)
                lblSaleText.text = value;
            lblSaleText.scale = new Vector3(textSize,textSize,lblSaleText.scale.z);
        }
    }

    public void SetTextWithFormatString(params object[] objects)
    {
        if (!lblSaleText)
        {
            Debug.LogErrorFormat("lblSaleText is not defined on object {0}",MiscTools.GetFullTransformName(transform));
            return;
        }
        try
        {
            formatString = formatString.Replace("\\n", "\n");
            lblSaleText.text = string.Format(formatString, objects);
        }
        catch(Exception ex)
        {
            Debug.LogErrorFormat("Cant SetText to label {0}! Error {1}", MiscTools.GetFullTransformName(transform), ex.Message);
            lblSaleText.text = "";
        }
        
    }

    public void SetActive(bool activate)
    {
        if (activate != sprSaleSticker.activeSelf)
            sprSaleSticker.SetActive(activate);
    }
}
