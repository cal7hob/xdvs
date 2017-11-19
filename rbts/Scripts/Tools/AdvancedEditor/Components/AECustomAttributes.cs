using System;
//using System.Runtime.InteropServices;

//[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
public sealed class AEDictionaryAttribute : Attribute { }

//[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
//[ComVisible(true)]
public sealed class AECustomAttributes : Attribute //public sealed class JNODGroupSettingsEditor : Editor
{
    public AECustomAttributes(eType type, params object[] params_)
    {
        this.type = type;
        this.params_ = params_;
    }

    public enum eType
    {
        NotSerializableParent,
        ListKey,
        ListItemName,
        DateTime
    }

    public eType type;
    public object[] params_;
}