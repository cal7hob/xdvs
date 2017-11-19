using UnityEditor;

public class AESerializedItemIList : AESerializedItem
{
    public AESerializedItemIList(AESerializedProperty parent, object object_, int id) : base(parent, object_)
    {
        this.id = id;
    }

    public int id = 0;

    public override string name
    {
        get
        {
            if (isClass)
            {
                if (name_ != null) return name_;
                return base.name;
            }
            return "Item " + id;
        }
    }

    public override SerializedProperty GetUnintySPChild(string name)
    {
        return parent.GetUnintySPChild(id.ToString()).FindPropertyRelative(name);
    }

    public override SerializedProperty GetUnintySP()
    {
        return parent.GetUnintySPChild(id.ToString());
    }
}