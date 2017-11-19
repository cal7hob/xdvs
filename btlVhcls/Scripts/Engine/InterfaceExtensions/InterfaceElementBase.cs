using UnityEngine;


public interface InterfaceElement
{
    Color GetColor();
    void SetColor(Color color);
    string GetText();
    void SetText(string text);
    Vector3 GetSize();
    void SetSize(Vector3 size);
}


/// <summary>
/// Класс создан, чтобы обобщить изменение текстмеша и спрайта
/// InterfaceElementBase можно накидывать на gameObject-ы (даже не содержащие Sprite / TextMesh) для изменения их LocalPosition через ConditionHelper
/// </summary>

public class InterfaceElementBase : MonoBehaviour, InterfaceElement
{
    public virtual Color GetColor() { return Color.white; }
    public virtual void SetColor(Color color) { }
    public virtual string GetText() { return ""; }
    public virtual void SetText(string text) { }
    public virtual Vector3 GetSize() { return Vector3.one; }
    public virtual void SetSize(Vector3 size) { }
}

