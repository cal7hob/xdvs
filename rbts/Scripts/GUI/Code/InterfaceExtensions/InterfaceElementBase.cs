using UnityEngine;


public interface InterfaceElement
{
    Color GetColor();
    void SetColor(Color color);
    string GetText();
    void SetText(string text);
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
}

