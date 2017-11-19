using System;
using UnityEngine;

public class BitMaskAttribute : PropertyAttribute
{
    public Type type;

    public BitMaskAttribute(Type type) { this.type = type; }
}