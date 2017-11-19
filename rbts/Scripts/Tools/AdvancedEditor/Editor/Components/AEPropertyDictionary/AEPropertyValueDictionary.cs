using System.Collections.Generic;

/// <summary>
/// fix GetEnumerator() for foreach
/// </summary>
public class AEPropertyValueDictionary : AEPropertyDictionary
{
    private static List<AESerializedProperty> values = new List<AESerializedProperty>();

    public override List<AESerializedProperty> Values
    {
        get
        {
            return values;
        }
    }

    public override List<AESerializedProperty>.Enumerator GetEnumerator()
    {
        return values.GetEnumerator();
    }
}