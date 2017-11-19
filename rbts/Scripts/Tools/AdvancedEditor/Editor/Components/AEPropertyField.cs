using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AEPropertyField
{
    public AEPropertyField()
    {
        Init();
    }

    public delegate void PrintValue(AESerializedProperty property, AEPropertyFieldSetting setting);
    public static Dictionary<Type, PrintValue> objectTypePrint;
    public static Dictionary<Type, PrintValue> customClassTypePrint;
    public Dictionary<Type, PropertyDrawer> drawerTypePrint = new Dictionary<Type, PropertyDrawer>();
    private static Dictionary<Type, object> typeToObjectDefault = new Dictionary<Type, object>()
    {
        { typeof(string),       ""          },
        { typeof(int),          0           },
        { typeof(float),        0.0f        }, //base type ValueType
        { typeof(bool),         false       },
        { typeof(byte),         default(byte)        },
        //{ typeof(GameObject),   null        },
    };

    private static Type typeObject = typeof(UnityEngine.Object);

    public virtual void Init()
    {
        objectTypePrint = new Dictionary<Type, PrintValue>()
        {
            { typeof(string),               PrintString         },
            { typeof(byte),                 PrintByte           },
            { typeof(int),                  PrintInt            },
            { typeof(float),                PrintFloat          }, //base type ValueType
            { typeof(long),                 PrintLong           },
            { typeof(uint),                 PrintUint           }, //add check negative values, http://forum.unity3d.com/threads/long-data-type-does-not-appear-in-inspector.28587/
            { typeof(bool),                 PrintBool           },
            { typeof(DateTime),             PrintDateTime       },
            { typeof(UnityEngine.Object),   PrintGameObject     },
            { typeof(Transform),            PrintGameObject     },
            { typeof(MonoBehaviour),        PrintGameObject     },
            { typeof(GameObject),           PrintGameObject     },
            { typeof(AudioClip),            PrintGameObject     },
            { typeof(Light),                PrintGameObject     },
            { typeof(MonoScript),           PrintGameObject     },
            { typeof(Texture),              PrintGameTexture    }, //fix for normal rendering textures use type UnityEngine.Object
            { typeof(Texture2D),            PrintGameTexture    },
            { typeof(Vector2),              PrintVector2        },
            { typeof(Vector3),              PrintVector3        },
            { typeof(Color),                PrintColor          },
            { typeof(HeaderAttribute),      PrintHeader         },
        };

        customClassTypePrint = new Dictionary<Type, PrintValue>()
        {
            { typeof(VectorI2),      PrintClassLine },
        };

        FindPropertyDrawer();
    }

    /*[System.Diagnostics.Conditional("UNITY_EDITOR")]
    public CustomPropertyDrawer CustomPropertyDrawer(Type type)
    {
        return new UnityEditor.CustomPropertyDrawer(type);
    }*/

    public void FindPropertyDrawer()
    {
        //int index = 0;
        Assembly assemblyCSharp = Assembly.Load("Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        Type findType = typeof(PropertyDrawer), typeProperty; //Type typeAttribute = typeof(CustomPropertyDrawer);
        string strTypeDrawer, strTypeProperty;
        int indexProperty, indexDrawer, indexPoint; //
        foreach (Type typeDrawer in Assembly.GetExecutingAssembly().GetTypes().Where(myType => myType.IsSubclassOf(findType))) //myType.IsClass && !myType.IsAbstract && // Assembly.GetAssembly(findType)
        {
            //index++;
            strTypeDrawer = typeDrawer.ToString();
            indexDrawer = strTypeDrawer.LastIndexOf("Drawer");
            indexProperty = strTypeDrawer.LastIndexOf("Property");
            indexPoint = strTypeDrawer.LastIndexOf(".");
            /*if (indexPoint == -1) indexPoint = 0;
            //Debug.Log("!!! " + indexPoint + " " + strTypeDrawer); //Debug.Log("!!! " + indexPoint + " " + indexProperty + " " + strTypeDrawer);
            string startStr = strTypeDrawer.Substring(0, indexPoint);
            strTypeProperty = strTypeDrawer.Substring(indexPoint, strTypeDrawer.Length - indexPoint);
            strTypeProperty = strTypeProperty.Replace("Drawer", "");
            strTypeProperty = strTypeProperty.Replace("Property", "");
            strTypeProperty = strTypeProperty.Replace("Editor", "");
            strTypeProperty = startStr + strTypeProperty;*/
            if (indexPoint != -1) continue;
            if (indexProperty != -1 && indexProperty < indexDrawer) indexDrawer = indexProperty; //if (indexProperty != -1 && indexProperty > indexPoint && indexProperty < indexDrawer) indexDrawer = indexProperty;
            if (indexDrawer == -1) continue;
            strTypeProperty = strTypeDrawer.Substring(0, indexDrawer); //strTypeProperty = indexPoint == -1 ? strTypeDrawer.Substring(0, indexDrawer) : strTypeDrawer.Substring(indexPoint + 1, indexDrawer - indexPoint - 1);

            if (strTypeDrawer == strTypeProperty)
            {
                //Debug.Log("!!! Not parse " + indexPoint + " " + strTypeDrawer);
                continue;
            }

            typeProperty = assemblyCSharp.GetType(strTypeProperty);
            if (typeProperty == null)
            {
                //Debug.Log("!!! Not get type " + indexPoint + " " + strTypeProperty);
                continue;
            }
                
            //Debug.Log("!!! " + typeDrawer + " " + typeProperty); //object[] objects = typeDrawer.GetCustomAttributes(typeAttribute, false);
            object objAtr = typeDrawer.GetCustomAttributes(true)[0];
            //PropertyInfo typeIdPI = objAtr.GetType().GetProperty("TypeId");
            //object obj = typeIdPI.GetValue(objAtr, null); //.GetProperty GetField
            //PropertyInfo namePI = typeIdPI.GetType().GetProperty("ReflectedType");
            //Debug.Log("!!! TypeId_: " + (namePI == null) + "  obj " + (obj == null));
            //Debug.Log("!!! TypeId.Name: " + namePI.GetValue(obj, null));
            //Debug.Log("!!! TypeId.Name: " + objAtr);
            //HotKeys.GetInfoType(objAtr.GetType());//typeDrawer.GetCustomAttributes(true)[0].GetType()
            //System.Reflection.MonoProperty
            drawerTypePrint.Add(typeProperty, (PropertyDrawer)Activator.CreateInstance(typeDrawer));
        }
    }

    public virtual void PropertyField(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        if (property.isHide) return;
        
        if (property.serializedObject.paintHandlers.ContainsKey(property.type))
        {
            property.serializedObject.paintHandlers[property.type](property, setting);
            return;
        }

        PrintObjectNotCustom(property, setting);
    }

    public virtual void PrintObjectNotCustom(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        if (property.isEnum)
        {
            PrintEnum(property, setting);
            return;
        }

        if (objectTypePrint.ContainsKey(property.type))
        {
            objectTypePrint[property.type](property, setting);
            return;
        }

        if (customClassTypePrint.ContainsKey(property.type))
        {
            customClassTypePrint[property.type](property, setting);
            return;
        }

        if (drawerTypePrint.ContainsKey(property.type))
        {
            PropertyDrawer propertyDrawer = drawerTypePrint[property.type];
            SerializedProperty serializedProperty = property.GetUnintySP();
            GUIContent content = property.GetNameGUIContent(setting.printName);
            Rect rect = property.serializedObject.GetRectNext(setting.manualRect, property.serializedObject.rectPosition.width, (int)propertyDrawer.GetPropertyHeight(serializedProperty, content));
            EditorGUI.BeginChangeCheck();
            propertyDrawer.OnGUI(rect, serializedProperty, content);
            if (EditorGUI.EndChangeCheck())
            {
                serializedProperty.serializedObject.ApplyModifiedProperties();
                property.Update();
            }
            return;
        }

        if (property.isList)
        {
            PrintIList(property, setting);
            return;
        }

        if (property.isDictionary)
        {
            PrintDictionary(property, setting);
            return;
        }

        if (property.isMonoBehaviour)
        {
            PrintGameObject(property, setting);
            return;
        }

        property.PropertyField(setting); //PrintClass(property, setting);
    }

    public virtual void PrintClass(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.foldout = EditorGUI.Foldout(property.serializedObject.GetRectNext(setting.manualRect), property.foldout, property.GetNameGUIContent(setting.printName), true); //property.Name
        if (!setting.printChild) return;
        if (!property.foldout) return;
        property.serializedObject.rectPosition.Tab();
        foreach (AESerializedProperty child in property)
        {
            PropertyField(child, setting);
        }
        property.serializedObject.rectPosition.TabEnd();
    }

    public static object GetDefaultObject(Type type)
    {
        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (typeToObjectDefault.ContainsKey(type)) return typeToObjectDefault[type];

        if (type.IsClass)
        {
            return Activator.CreateInstance(type);
        }

        //return default(Type); //not work for int
        return null; //typeToObjectDefault[type];
    }

    public virtual void PrintString(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = EditorGUI.TextField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.stringValue);
    }

    public virtual void PrintHeader(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        EditorGUI.LabelField(property.serializedObject.GetRectNext(setting.manualRect), property.stringValue, GUI.skin.GetStyle("IN TitleText"));
    }

    public virtual void PrintByte(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = Convert.ToByte(EditorGUI.IntField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.byteValue));
    }

    public virtual void PrintInt(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        if (property.rangeAttribute == null)
        {
            property.valueGUIChanged = EditorGUI.IntField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.intValue);
        }
        else
        {
            property.valueGUIChanged = EditorGUI.IntSlider(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.intValue, (int)property.rangeAttribute.min, (int)property.rangeAttribute.max);
        }
    }

    public virtual void PrintFloat(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        if (property.rangeAttribute == null)
        {
            property.valueGUIChanged = EditorGUI.FloatField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.floatValue);
        }
        else
        {
            property.valueGUIChanged = EditorGUI.Slider(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.floatValue, property.rangeAttribute.min, property.rangeAttribute.max);
        }
    }

    public virtual void PrintLong(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = EditorGUI.LongField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.longValue);
    }

    public virtual void PrintUint(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = (uint)EditorGUI.LongField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.uintValue);
    }

    /*public virtual void PrintLong(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        DateTime dt = new DateTime((long)property.valueGUIChanged);
        //DateTime dt_ = new DateTime(2016, 4, 1);// new DateTime(1641404964);
        //DateTime dt = new DateTime(dt_.Ticks);
        EditorGUI.LabelField(property.serializedObject.rectPosition.Next(100, false), property.GetNameGUIContent(setting.printName));
        property.serializedObject.rectPosition.startX = 150;
        int day = EditorGUI.IntField(property.serializedObject.rectPosition.NextLine(20), GUIContent.none, dt.Day);
        int month = EditorGUI.IntField(property.serializedObject.rectPosition.NextLine(20), GUIContent.none, dt.Month);
        int year = EditorGUI.IntField(property.serializedObject.rectPosition.NextLine(40), GUIContent.none, dt.Year);
        property.valueGUIChanged = (new DateTime(year, month, day)).Ticks;
    }*/

    public virtual void PrintBool(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = EditorGUI.Toggle(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.boolValue);
    }

    public virtual void PrintEnum(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = EditorGUI.EnumPopup(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.enumValue);
    }

    public virtual void PrintDateTime(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        //CL.Log("type = " + property.type + " " + property.value);
        DateTime dt = property.GetValue<DateTime>();//new DateTime((long)property.valueGUIChanged);
        //DateTime dt_ = new DateTime(2016, 4, 1);// new DateTime(1641404964);
        //DateTime dt = new DateTime(dt_.Ticks);
        EditorGUI.LabelField(property.serializedObject.rectPosition.Next(100, false), property.GetNameGUIContent(setting.printName));
        property.serializedObject.rectPosition.startX = 150;
        int day = EditorGUI.IntField(property.serializedObject.rectPosition.NextLine(20), GUIContent.none, dt.Day);
        int month = EditorGUI.IntField(property.serializedObject.rectPosition.NextLine(20), GUIContent.none, dt.Month);
        int year = EditorGUI.IntField(property.serializedObject.rectPosition.NextLine(40), GUIContent.none, dt.Year);
        property.valueGUIChanged = new DateTime(year, month, day);//(new DateTime(year, month, day)).Ticks;
    }

    public virtual void PrintGameObject(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = EditorGUI.ObjectField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.unityObjectValue, property.type, true);
    }

    public virtual void PrintGameTexture(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        //fix for normal rendering textures use type UnityEngine.Object (typeObject)
        property.valueGUIChanged = EditorGUI.ObjectField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.unityObjectValue, typeObject, true);
    }

    public virtual void PrintClassLine(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        if (setting.printName) EditorGUI.LabelField(property.serializedObject.rectPosition.NextLine(50), property.Name);

        AEPropertyFieldSetting settingLine = new AEPropertyFieldSetting(setting);
        settingLine.printName = false;
        settingLine.manualRect = true;
        foreach (AESerializedProperty child in property)
        {
            EditorGUI.LabelField(property.serializedObject.rectPosition.NextLine(12), child.Name);
            property.serializedObject.rectPosition.NextLine(30);
            PropertyField(child, settingLine);
        }
    }

    public virtual void PrintVector2(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        //property.valueGUIChanged = EditorGUI.Vector2Field(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.vector2Value);
        property.serializedObject.GetRectNext(setting.manualRect);
        Rect thisRect = property.serializedObject.rectPosition.GetRect();
        if (setting.printName)
        {
            int width50p = (int)thisRect.width / 2;
            EditorGUI.LabelField(new Rect(thisRect.x, thisRect.y, width50p, thisRect.height), property.Name);
            property.valueGUIChanged = EditorGUI.Vector2Field(new Rect(thisRect.x + width50p, thisRect.y, width50p, thisRect.height), GUIContent.none, property.vector2Value);
        }
        else
        {
            property.valueGUIChanged = EditorGUI.Vector2Field(property.serializedObject.GetRectNext(setting.manualRect), GUIContent.none, property.vector2Value);
        }
    }

    public virtual void PrintVector3(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        //property.valueGUIChanged = EditorGUI.Vector3Field(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.vector3Value);
        property.serializedObject.GetRectNext(setting.manualRect);
        Rect thisRect = property.serializedObject.rectPosition.GetRect();
        if (setting.printName)
        {
            int width50p = (int)thisRect.width / 2;
            EditorGUI.LabelField(new Rect(thisRect.x, thisRect.y, width50p, thisRect.height), property.Name);
            property.valueGUIChanged = EditorGUI.Vector3Field(new Rect(thisRect.x + width50p, thisRect.y, width50p, thisRect.height), GUIContent.none, property.vector3Value);
        }
        else
        {
            property.valueGUIChanged = EditorGUI.Vector3Field(thisRect, GUIContent.none, property.vector3Value);
        }
    }

    public virtual void PrintColor(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.valueGUIChanged = EditorGUI.ColorField(property.serializedObject.GetRectNext(setting.manualRect), property.GetNameGUIContent(setting.printName), property.colorValue);
    }

//=============================================================================================================================
//Print IList
//=============================================================================================================================
    public void Clear(object obj)
    {
        ((AESerializedProperty)obj).Clear();
    }

    public void Add(object obj)
    {
        ((AESerializedProperty)obj).Add();
    }

    public void Copy(object obj)
    {
        AESerializedProperty property = (AESerializedProperty)obj;
        property.serializedObject.buffer = property;
    }

    public void ListPaste(object obj)
    {
        AESerializedProperty property = (AESerializedProperty)obj;
        if (property.serializedObject.buffer != null && property.serializedObject.buffer.itemType == property.itemType)
        {
            foreach (AESerializedProperty property_ in property.serializedObject.buffer)
            {
                property_.CloneProperty(property.Add()); //property.Add()
            }
        }
    }

    public void ListPasteTop(object obj)
    {
        AESerializedProperty property = (AESerializedProperty)obj;
        if (property.serializedObject.buffer != null && property.serializedObject.buffer.itemType == property.itemType)
        {
            List<AESerializedProperty> newItems = new List<AESerializedProperty>();
            for (int s = 0; s < property.serializedObject.buffer.count; s++) newItems.Add(property.AddTop());
            newItems.Reverse();
            int i = 0;
            foreach (AESerializedProperty property_ in property.serializedObject.buffer)
            {
                property_.CloneProperty(newItems[i]);
                i++;
            }
        }
    }

    public virtual void ListMenu(AESerializedProperty property)
    {
        Event evt = Event.current;
        if (evt.type == EventType.ContextClick)
        {
            if (property.serializedObject.rectPosition.GetRect().Contains(evt.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy"), false, Copy, property);
                menu.AddItem(new GUIContent("Paste"), false, ListPaste, property);
                menu.AddItem(new GUIContent("Paste Top"), false, ListPasteTop, property);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Add item"), false, Add, property);
                menu.AddItem(new GUIContent("Clear"), false, Clear, property);
                menu.ShowAsContext();
                evt.Use();
            }
        }
    }

    public void ItemPasteTop(object obj)
    {
        AESerializedProperty property = (AESerializedProperty)obj;
        if (property.serializedObject.buffer != null && property.serializedObject.buffer.type == property.type)
        {
            AESerializedProperty item = property.parent.Add();
            property.serializedObject.buffer.CloneProperty(item);
            item.Move(((AESerializedItemIList)property).id);
        }
    }

    public void Duplicate(object obj)
    {
        AESerializedProperty property = (AESerializedProperty)obj;
        AESerializedProperty item = property.parent.Add();
        property.CloneProperty(item);
        //item.Move(((AESerializedItemIList)property).id);
    }

    public void ItemRemove(object obj)
    {
        AESerializedProperty property = (AESerializedProperty)obj;
        property.ListRemoveThisItem();
    }

    public virtual void ListItemMenu(AESerializedProperty property)
    {
        Event evt = Event.current;
        if (evt.type == EventType.ContextClick)
        {
            if (property.serializedObject.rectPosition.GetRect().Contains(evt.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Duplicate"), false, Duplicate, property);
                menu.AddItem(new GUIContent("Copy"), false, Copy, property);
                menu.AddItem(new GUIContent("Paste Top"), false, ItemPasteTop, property);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Remove"), false, ItemRemove, property);
                menu.ShowAsContext();
                evt.Use();
            }
        }
    }

    public virtual void PrintIList(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        property.foldout = EditorGUI.Foldout(property.serializedObject.GetRectNext(setting.manualRect, property.serializedObject.rectPosition.width - 46/*25*/), property.foldout, property.Name, true); //-width button
        /*int button = property.serializedObject.rectPosition.startX + property.serializedObject.rectPosition.width - 100;//75 property.serializedObject.rectPosition.width //property.serializedObject.rectPosition.startX
        property.foldout = EditorGUI.Foldout(property.serializedObject.GetRectNext(setting.manualRect, button), property.foldout, property.Name, true);*/

        ListMenu(property);
        if (!setting.printChild) return;
        if (!property.foldout) return;
        property.serializedObject.rectPosition.Tab();

        //if (GUI.Button(property.serializedObject.rectPosition.NextRelative(19), "+")) property.Add();
        
        AESerializedIList propertyIList = (AESerializedIList)property;
        Event evt = Event.current;

        Rect rect = property.serializedObject.rectPosition.NextRelative(40); //19
        if (evt.type == EventType.MouseUp) propertyIList.isSelect = rect.Contains(evt.mousePosition); // && evt.keyCode == KeyCode.Mouse0
        if (!propertyIList.isSelect) propertyIList.editCount = property.count;
        propertyIList.editCount = EditorGUI.IntField(rect, GUIContent.none, propertyIList.editCount);
        if (propertyIList.isSelect && evt.type == EventType.KeyUp && evt.keyCode == KeyCode.Return) property.count = propertyIList.editCount;

        if (propertyIList.moveItem == null)
        {
            foreach (AESerializedProperty item in property)
            {
                //if (GUI.Button(property.serializedObject.rectPosition.NextNotSetExit(19), "-")) item.ListRemoveThisItem();
                property.serializedObject.rectPosition.Next(property.serializedObject.rectPosition.width); // - 19

                if (property.serializedObject.rectPosition.NotSetStart(10, property.serializedObject.rectPosition.height, -property.serializedObject.rectPosition.tab).Contains(evt.mousePosition))
                {
                    if (evt.type == EventType.mouseDown) propertyIList.moveItem = item;
                    EditorGUI.LabelField(property.serializedObject.rectPosition.NotSetStart(10, property.serializedObject.rectPosition.height, -property.serializedObject.rectPosition.tab), "=");
                }
                item.serializedObject.ChangeManualRectOne();
                PropertyField(item, setting);
                ListItemMenu(item);
            }
            if (EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.Repaint();
        }
        else
        {
            int startPosition = property.serializedObject.rectPosition.startY;
            AEPropertyFieldSetting settingHiden = new AEPropertyFieldSetting(setting.printName, false, true);
            foreach (AESerializedProperty item in property)
            {
                if (propertyIList.moveItem != item)
                {
                    if (GUI.Button(property.serializedObject.rectPosition.NextNotSetExit(19), "-")) item.ListRemoveThisItem();
                    property.serializedObject.rectPosition.Next(property.serializedObject.rectPosition.width - 19);
                    PropertyField(item, settingHiden);
                    //ListItemMenu(item);
                }
            }
            property.serializedObject.rectPosition.rect.y = evt.mousePosition.y - property.serializedObject.rectPosition.rect.height / 2;
            //EditorGUI.LabelField(property.serializedObject.rectPosition.GetRect(), "            " + (property.serializedObject.rectPosition.rect.y - startPosition) + " " + (int)((property.serializedObject.rectPosition.rect.y - startPosition) / (property.serializedObject.rectPosition.height + property.serializedObject.rectPosition.separator)));
            if (evt.type == EventType.mouseUp)
            {
                propertyIList.moveItem.Move((int)((property.serializedObject.rectPosition.rect.y - startPosition) / (property.serializedObject.rectPosition.height + property.serializedObject.rectPosition.separator)));
                propertyIList.moveItem = null;
            }
            else
            {
                PropertyField(propertyIList.moveItem, settingHiden);
                if (EditorWindow.focusedWindow != null) EditorWindow.focusedWindow.Repaint();
            }
        }
        property.serializedObject.rectPosition.TabEnd();
    }

    public virtual void PrintDictionary(AESerializedProperty property, AEPropertyFieldSetting setting)
    {
        int buttonWidth = 25;
        int keyWidth = (property.serializedObject.rectPosition.width - property.serializedObject.rectPosition.separator) / 2;
        int nameDictionaryWidth = property.serializedObject.rectPosition.width - keyWidth;
        int valueStartX = keyWidth + property.serializedObject.rectPosition.separator;

        property.foldout = EditorGUI.Foldout(property.serializedObject.GetRectNext(setting.manualRect, nameDictionaryWidth), property.foldout, property.Name, true);
        //ListMenu(property);
        if (!setting.printChild) return;
        if (!property.foldout) return;

        if (!property.itemKeyIsClass)
        {
            property.serializedObject.rectPosition.startX = nameDictionaryWidth;//175 200
            property.serializedObject.rectPosition.NextLine(keyWidth - buttonWidth);
            property.serializedObject.ChangeManualRectOne();
            property.serializedObject.ChangeOffPrintNameOne();
            PropertyField(property.itemNewKey, setting);
        }
        //if (!property.type.Equals(typeof(string))) 
        property.serializedObject.rectPosition.Tab();
        if (GUI.Button(property.serializedObject.rectPosition.NextRelative(buttonWidth), "+")) property.DictAdd(property.itemNewKey.valueGUIChanged);

        foreach (AESerializedProperty item in property)
        {
            if (GUI.Button(property.serializedObject.rectPosition.NextNotSetExit(buttonWidth), "-")) item.ListRemoveThisItem();

            property.serializedObject.rectPosition.Next(keyWidth);
            property.serializedObject.ChangeManualRectOne();
            if (!property.itemKeyIsClass) property.serializedObject.ChangeOffPrintNameOne();
            PropertyField(item.dictKeyItem, setting);

            if (item.dictKeyItem.isClass && item.dictKeyItem.foldout) continue;
            property.serializedObject.rectPosition.startX = valueStartX; //property.serializedObject.rectPosition.UpdateRect();
            property.serializedObject.rectPosition.NextLine(keyWidth);
            property.serializedObject.ChangeManualRectOne();
            if (!property.itemIsClass) property.serializedObject.ChangeOffPrintNameOne();
            PropertyField(item, setting);
        }
        property.serializedObject.rectPosition.TabEnd();
    }
}
