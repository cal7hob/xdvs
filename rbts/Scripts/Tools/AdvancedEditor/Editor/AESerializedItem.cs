using System;

public class AESerializedItem : AESerializedClass //not fieldInfo
{
    public AESerializedItem() { }

    public AESerializedItem(AESerializedProperty parent, object object_)
    {
        this.parent = parent;
        this.objectParse = object_;
        this.serializedObject = parent.serializedObject;
        Init();
        if (isClass) Parse();
    }

    protected bool isClassCache = false;
    protected Type typeCache;

    public override void Init()
    {
        properties = new AEPropertyClassDictionary();
        typeCache = objectParse == null ? parent.itemType : objectParse.GetType() ; //temp fix objectParse == null
        isClassCache = isClass_;
    }

    public override Type type
    {
        get
        {
            return typeCache;
        }
    }

    public override string name
    {
        get
        {
            AESerializedProperty property = GetProperty("name", true);
            if (property == null) property = GetProperty("text", true);
            if (property == null) return type.ToString();
            if (property.value == null) return type.ToString();
            if (property.value.ToString() == "") return type.ToString();
            return property.value.ToString();
        }
    }

    public override bool SetValue(object value)
    {
        if (objectParse == null)
        {
            if (objectParse == value) return false;
        }
        else
        {
            if (objectParse.Equals(value)) return false;
        }

        if (type.IsEnum)
        {
            if (value.Equals((int)objectParse)) return false;
            objectParse = Enum.ToObject(type, value);
        }
        else
        {
            objectParse = value;
        }

        return true;
    }

    public override bool isClass
    {
        get
        {
            return isClassCache;
        }
    }

    public override bool itemIsMonoBehaviour
    {
        get
        {
            return CheckMonoBehaviour(itemType);
        }
    }

    public override int countVisible
    {
        get
        {
            if (!isClass) return 1;
            return base.countVisible;
        }
    }

    public override void CloneProperty(AESerializedProperty fieldObject)
    {
        if (isClass)
        {
            base.CloneProperty(fieldObject);
            return;
        }

        if (value == null) return;
        fieldObject.value = value;
    }
}
