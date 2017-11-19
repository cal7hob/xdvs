using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AESerializedIList : AESerializedClass
{
    public AESerializedIList(AESerializedProperty parent, FieldInfo fieldInfo) : base(parent, fieldInfo) { }

    public List<int> trashId = new List<int>();
    public int id = 0;
    public bool isSelect = false;
    public int editCount = 0;
    public AESerializedProperty moveItem;
    public string keyName;

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public override void Init()
    {
        properties = new AEPropertyListDictionary();
    }

    public virtual void ChangeKey(AESerializedProperty property)
    {
        properties.Keys[properties.Values.IndexOf(property.parent)] = property.value;
        ((AESerializedClass)property.parent).nameSet = property.value.ToString();
    }

//=============================================================================================================================
//clone object
//=============================================================================================================================
    public override void CloneProperty(AESerializedProperty fieldObject)
    {
        if (value == null) return;
        if (fieldObject.type != type)
        {
            Debug.Log(this.GetType() + ": fieldObject.type != type");
            return;
        }

        if (!fieldObject.isList)
        {
            Debug.Log(this.GetType() + ": fieldObject not list " + Name + " " + type + " " + PathDebug());
            return;
        }

        if (fieldObject.value == null)
        {
            fieldObject.value = Activator.CreateInstance(fieldObject.type);
            fieldObject.Parse();
        }

        fieldObject.Clear();
            
        foreach (AESerializedProperty property in this)
        {
            property.CloneProperty(fieldObject.Add());
        }
    }

//=============================================================================================================================
//properties object
//=============================================================================================================================
    public override bool isClass
    {
        get
        {
            return false;
        }
    }
    
//=============================================================================================================================
//List
//=============================================================================================================================
    public override bool itemIsClass// not working
    {
        get
        {
            Type itemType_ = itemType;
            return itemType_.IsClass && !AEPropertyField.objectTypePrint.ContainsKey(itemType_);
        }
    }

    public override int count
    {
        set
        {
            if (value < count)//while
            {
                ListRemove(properties[count - 1]);
                Synchronize();
                count = value;
                return;
            }

            if (value > count)
            {
                if (count > 0)
                {
                    properties[count - 1].CloneProperty(Add());
                }
                else
                {
                    Add();
                }
                count = value;
            }
        }
    }

    private bool itemIsMonoBehaviour_;
    private bool itemIsMonoBehaviour_NotInit = true;
    public override bool itemIsMonoBehaviour
    {
        get
        {
            if (itemIsMonoBehaviour_NotInit)
            {
                itemIsMonoBehaviour_ = CheckMonoBehaviour(itemType);
                itemIsMonoBehaviour_NotInit = false;
            }
            return itemIsMonoBehaviour_;
        }
    }

    public override void Clear()
    {
        base.Clear();
        id = 0;
    }

    protected object GetItemDefaultObject()
    {
        Type itemType_ = itemType;
        if (itemIsMonoBehaviour) return null;
        return AEPropertyField.GetDefaultObject(itemType_);
    }

    public override UnityEditor.SerializedProperty GetUnintySPChild(string name)
    {
        return parent.GetUnintySPChild(this.name).GetArrayElementAtIndex(int.Parse(name));
    }

    private AETextureDictionary textures;
    public AESerializedProperty selectItem;

    public void ListButtons(Color color, int width, int height, string imagesPath, bool printChild = false) //bool printName = true
    {
        if (textures == null) textures = new AETextureDictionary();
        ListButtonsTop();
        foreach (AESerializedProperty item in this)
        {
            //if (AEEditorTools.ColorButton(serializedObject.rectPosition.NextLine(width, height), textures[imagesPath + Path.DirectorySeparatorChar + item.name + ".PNG"], item == selectItem, color)) if (selectItem == item) selectItem = null; else selectItem = item;
            if (AEEditorTools.ColorButton(serializedObject.rectPosition.NextLine(width, height), new GUIContent(item.name, textures[imagesPath + Path.DirectorySeparatorChar + item.name + ".PNG"]), item == selectItem, color)) if (selectItem == item) selectItem = null; else selectItem = item;
        }
        ListButtonsBottom(printChild);
    }

    public void ListButtons(Color color, int width, bool printChild = false)
    {
        ListButtonsTop();
        serializedObject.rectPosition.Next();
        foreach (AESerializedProperty item in this)
        {
            if (AEEditorTools.ColorButton(serializedObject.rectPosition.NextLine(width), item.name, item == selectItem, color)) if (selectItem == item) selectItem = null; else selectItem = item;
        }
        ListButtonsBottom(printChild);
    }

    public void ListButtons(Color color, bool printChild = false)
    {
        ListButtonsTop(); //ListButtons(color, serializedObject.rectPosition.windowWidth - serializedObject.rectPosition.separator, printChild);
        int width = serializedObject.rectPosition.windowWidth - serializedObject.rectPosition.separator;
        foreach (AESerializedProperty item in this)
        {
            if (AEEditorTools.ColorButton(serializedObject.rectPosition.Next(width), item.name, item == selectItem, color)) if (selectItem == item) selectItem = null; else selectItem = item;
        }
        ListButtonsBottom(printChild);
    }

    private void ListButtonsTop()
    {
        serializedObject.rectPosition.Next();
        GUI.Label(serializedObject.rectPosition.NextLine((int)(GUI.skin.label.CalcSize(new GUIContent(name))).x), name); // width - 150
        int widthButton = serializedObject.rectPosition.height + serializedObject.rectPosition.separator;
        if (GUI.Button(serializedObject.rectPosition.NextLine(widthButton), "+")) { if (selectItem == null) Add(); else selectItem.CloneProperty(Add()); }
        if (GUI.Button(serializedObject.rectPosition.NextLine(widthButton), "-") && selectItem != null) { selectItem.ListRemoveThisItem(); Synchronize(); if (count > 0) selectItem = properties.Values[count - 1]; }

        serializedObject.rectPosition.Tab();
    }

    private void ListButtonsBottom(bool printChild)
    {
        serializedObject.rectPosition.Tab();
        if (printChild && selectItem != null) selectItem.PropertyFieldChilds(true, true, false);
        serializedObject.rectPosition.TabEnd();
        serializedObject.rectPosition.TabEnd();
    }
}
