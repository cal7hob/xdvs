using System.Collections.Generic;
using UnityEngine;

public class KeyboardUIItem : MonoBehaviour
{
    public tk2dTextMesh label;
    public tk2dBaseSprite activator;
    public List<UserRemapKeyboard.KeyUIItem> keys = new List<UserRemapKeyboard.KeyUIItem>();

    public static KeyboardUIItem Create(string name, int itemPositionY)
    {
        KeyboardUIItem keyboardItem = Instantiate(UserRemapKeyboard.instance.keyboardItemPrefab);
        keyboardItem.transform.parent = UserRemapKeyboard.instance.scrollableArea.contentContainer.transform;
        keyboardItem.transform.localPosition = new Vector3(130, itemPositionY, 1);
        keyboardItem.name = name;
        keyboardItem.label.name = "lbl" + name; //for localize
        keyboardItem.Init();
        keyboardItem.gameObject.SetActive(true);
        if (keyboardItem.label.text == string.Empty) keyboardItem.label.text = name;
        return keyboardItem;
    }

    private void Init()
    {
        foreach (UserRemapKeyboard.KeyUIItem keyUIItem in keys) keyUIItem.Init(activator);
    }
}