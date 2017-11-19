using System;
using System.Collections;
using System.Reflection;

public class AESerializedArray : AESerializedIList
{
    public AESerializedArray(AESerializedProperty parent, FieldInfo fieldInfo) : base(parent, fieldInfo) { }
    public Array valueArray;

//=============================================================================================================================
//Parser
//=============================================================================================================================
    public override void Init()
    {
        base.Init();
        if (value == null)
        {
            value = Array.CreateInstance(itemType, 0);
        }
        valueArray = (Array)value;
    }

    public override void Parse()
    {
        // Add: Update, Add, Synchronize
        AESerializedItemArray serializedItemArray;
        if (CustomAttributeIsDefined(typeof(AECustomAttributes)))
        {
            object[] params_ = GetCustomAttributeParams(AECustomAttributes.eType.ListKey);
            if (params_ != null)
            {
                keyName = (string)params_[0];

                AESerializedProperty serializedKey;
                foreach (object item in valueArray)
                {
                    serializedItemArray = new AESerializedItemArray(this, item, id);
                    serializedItemArray.isMonoBehaviour__ = itemIsMonoBehaviour;
                    serializedKey = serializedItemArray[keyName];
                    properties.Add(serializedKey.value, serializedItemArray);
                    serializedItemArray.nameSet = serializedKey.value.ToString();
                    serializedObject.SubscribeChangeProperty(ChangeKey, serializedKey);
                    id++;
                }
                return;
            }
        }

        foreach (object item in valueArray)
        {
            serializedItemArray = new AESerializedItemArray(this, item, id);
            properties.Add(id, serializedItemArray);
            serializedItemArray.isMonoBehaviour__ = itemIsMonoBehaviour;
            id++;
        }
    }

    public override void Update()
    {
        this.objectParse = fieldInfo.GetValue(this.parent.objectParse); //for update Array
        valueArray = (Array)value;

        if (!CheckChangeItems())
        {
            base.Update();
            return;
        }

        AEPropertyDictionary newProperties = new AEPropertyListDictionary();
        AESerializedProperty property;
        id = 0;
        foreach (object item in objectParse as IList)
        {
            property = properties.GetProperty(item);
            if (property  == null)
            {
                AESerializedItemArray itemProperty = new AESerializedItemArray(this, item, id);
                itemProperty.isMonoBehaviour__ = itemIsMonoBehaviour;
                newProperties.Add(id, itemProperty);
                //Debug.Log(this.GetType() + ": new item " + Name + " " + id + " " + properties.GetType());
                //Debug.Log(this.GetType() + ": new item " + Name + " " + id + " " + properties.GetType() + " " + item.GetHashCode() + " old " + properties[id.ToString()].value.GetHashCode());
            }
            else
            {
                //Debug.Log(this.GetType() + ": item " + Name + " " + id + " " + properties.GetType());
                //Debug.Log(this.GetType() + ": item " + Name + " " + id + " " + properties.GetType() + " " + item.GetHashCode() + " old " + properties[id.ToString()].value.GetHashCode());
                newProperties.Add(id, property);
                ((AESerializedItemArray)property).id = id;
                property.Update();
            }
            id++;
        }
        properties = newProperties;
    }

    bool CheckChangeItems()
    {
        if (valueArray.Length != properties.Count) return true;
        foreach (object item in valueArray)
        {
            if (properties.GetProperty(item) == null) return true;
        }
        return false;
    }

//=============================================================================================================================
//properties object
//=============================================================================================================================

    public override bool isList
    {
        get
        {
            return true;
        }
    }

    public override bool SetValue(object value)
    {
        if (valueArray != value)
        {
            valueArray = (Array)value;
            objectParse = value;
            fieldInfo.SetValue(parent.value, value);
            //Debug.Log(this.GetType() + ": set array value");
            return true;
        }
        return false;
    }

    public override bool isArray
    {
        get
        {
            return type.IsArray;
        }
    }

//=============================================================================================================================
//List
//=============================================================================================================================
    public override AESerializedItem AddTop()
    {
        return AddTop(GetItemDefaultObject());
    }

    public override AESerializedItem AddTop(object value)
    {
        Array newArray = Array.CreateInstance(itemType, valueArray.Length + 1);
        valueArray.CopyTo(newArray, 1);

        newArray.SetValue(value, 0); //newArray.Length - 1 //valueArray.Length
        //Debug.Log(this.GetType() + ": array Add count " + newArray.Length);
        AESerializedItem item = new AESerializedItemArray(this, value, id);
        item.isMonoBehaviour__ = itemIsMonoBehaviour;

        AEPropertyDictionary oldProperties = properties;
        properties = new AEPropertyListDictionary();

        id = 0;
        if (keyName != null)
        {
            serializedObject.SubscribeChangeProperty(ChangeKey, item[keyName]);
            properties.Add(item[keyName].value, item);
        }
        else
        {
            properties.Add(id, item);
        }
        id++;
        
        foreach (AESerializedItemArray property in oldProperties)
        {
            properties.Add(id, property);
            property.id = id;
            id++;
        }

        this.value = newArray;

        return item;
    }
    
    public override AESerializedItem Add()
    {
        return Add(GetItemDefaultObject());
    }

    public override AESerializedItem Add(object value)
    {
        Array newArray = Array.CreateInstance(itemType, valueArray.Length + 1);
        valueArray.CopyTo(newArray, 0);

        newArray.SetValue(value, valueArray.Length); //newArray.Length - 1 //valueArray.Length
        AESerializedItem item = new AESerializedItemArray(this, value, id);
        item.isMonoBehaviour__ = itemIsMonoBehaviour;
        if (keyName != null)
        {
            serializedObject.SubscribeChangeProperty(ChangeKey, item[keyName]);
            properties.Add(item[keyName].value, item);
        }
        else
        {
            properties.Add(id, item);
        }
        id++;

        this.value = newArray;

        return item;
    }

    public override Type itemType
    {
        get
        {
            return type.GetElementType();
        }
    }

    public override void ListRemove(AESerializedProperty property)
    {
        AESerializedItemArray propertyItem = (AESerializedItemArray)property;
        trashId.Add(propertyItem.id);
        propertyItem.id = int.MaxValue;
    }

    public override void Clear()
    {
        base.Clear();
        valueArray = Array.CreateInstance(itemType, 0);
        value = valueArray;
    }

    public override bool Synchronize()
    {
        if (trashId.Count > 0) // safe remove for foreach
        {
            Array newArray = Array.CreateInstance(itemType, valueArray.Length - trashId.Count);

            AEPropertyDictionary propertiesSort = new AEPropertyListDictionary(); // temp fix (add in AEPropertyDictionary create or synhronize)
            id = 0;
            foreach (AESerializedItemArray property in properties)
            {
                if (property.id != int.MaxValue)
                {
                    propertiesSort.Add(id, property);
                    newArray.SetValue(property.value, id);
                    property.id = id;
                    id++;
                }
            }

            value = newArray;
            properties = propertiesSort;
            trashId.Clear();
            return true;
        }
        return false;
    }
}
