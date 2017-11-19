public static class ObjectExtensions
{
    /// <summary>
    /// Проверка на null в обход юнитевского оверрайда оператора ==.
    /// </summary>
    /// <typeparam name="TSource">Тип объекта.</typeparam>
    /// <param name="source">Объект.</param>
    /// <returns>Результат проверки на null.</returns>
    public static bool IsSystemNull<TSource>(this TSource source)
    {
        return source == null;
    }
}
