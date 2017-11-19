using UnityEngine;

public static class Vector3Extensions
{
    /// <summary>
    /// Получить нормализованный вектор с обнулённой длиной по оси Y.
    /// </summary>
    /// <param name="source">Исходное значение Vector3.</param>
    /// <returns>Нормализованный вектор с обнулённой длиной по оси Y.</returns>
    public static Vector3 GetHorizontalIdentity(this Vector3 source) { return new Vector3(source.x, 0.0f, source.z).normalized; }

    /// <summary>
    /// Получить нормализованный вектор с обнулённой длиной по осям X и Z.
    /// </summary>
    /// <param name="source">Исходное значение Vector3.</param>
    /// <returns>Нормализованный вектор с обнулённой длиной по осям X и Z.</returns>
    public static Vector3 GetVerticalIdentity(this Vector3 source) { return new Vector3(0.0f, source.y, 0.0f).normalized; }
}
