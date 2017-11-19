using System;
using UnityEngine;
using UnityEditor;

public static class BitMaskDrawer
{
    public static int Draw(Rect position, int mask, Type type, GUIContent label)
    {
        string[] itemNames = Enum.GetNames(type);
        int[] itemValues = (int[]) Enum.GetValues(type);

        int val = mask;
        int maskVal = 0;

        for (int i = 0; i < itemValues.Length; i++)
        {
            if (itemValues[i] == 0)
            {
                if (val == 0)
                    maskVal |= 1 << i;
            }
            else
            {
                if ((val & itemValues[i]) == itemValues[i])
                    maskVal |= 1 << i;
            }
        }

        int newMaskVal = EditorGUI.MaskField(position, label, maskVal, itemNames);
        int changes = maskVal ^ newMaskVal;

        for (int i = 0; i < itemValues.Length; i++)
        {
            if ((changes & (1 << i)) == 0)
                continue;

            if ((newMaskVal & (1 << i)) != 0)
            {
                if (itemValues[i] == 0)
                {
                    val = 0;
                    break;
                }

                val |= itemValues[i];
            }
            else
            {
                val &= ~itemValues[i];
            }
        }

        return val;
    }
}

[CustomPropertyDrawer(typeof(BitMaskAttribute))]
public class EnumBitMaskPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        BitMaskAttribute bitMaskAttribute = (BitMaskAttribute) attribute;
        property.intValue = BitMaskDrawer.Draw(position, property.intValue, bitMaskAttribute.type, label);
    }
}