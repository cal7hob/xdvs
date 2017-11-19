//using UnityEngine.Advertisements;

/// <summary>
/// Примесь функций для наследников IAdsService.
/// </summary>
public static class AdsServiceExtensions
{
    /// <summary>
    /// Отправка события Google Analytics. Вызывается внутри класса конкретного сервиса.
    /// </summary>
    /// <param name="source">Вызывающий объект.</param>
    /// <param name="service">Ключ субъекта, означающий сервис поставщика.</param>
    /// <param name="result">Ключ действия, означающий результат показа рекламы.</param>
    /// <param name="zone">Ключ лейбла, означающий зону показа (стандарт или за вознаграждение).</param>
    public static void ReportGAEvent(
        this IAdsService    source,
        GAEvent.Subject     service,
        GAEvent.Action      result,
        GAEvent.Label       zone)
    {
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.Advertisement)
                .SetSubject(service)
                .SetParameter(result)
                .SetParameter(zone)
                .SetValue(ProfileInfo.Level));
    }

    /// <summary>
    /// Отправка события Google Analytics. Вызывается внутри класса конкретного сервиса.
    /// </summary>
    /// <param name="source">Вызывающий объект.</param>
    /// <param name="service">Ключ субъекта, означающий сервис поставщика.</param>
    /// <param name="result">Ключ действия, означающий результат показа рекламы.</param>
    /// <param name="zone">Ключ лейбла, означающий зону показа (стандарт или за вознаграждение).</param>
    //public static void ReportGAEvent(
    //    this IAdsService    source,
    //    GAEvent.Subject     service,
    //    ShowResult          result,
    //    GAEvent.Label       zone)
    //{
    //    GoogleAnalyticsWrapper.LogEvent(
    //        new CustomEventHitBuilder()
    //            .SetParameter(GAEvent.Category.Advertisement)
    //            .SetSubject(service)
    //            .SetParameter(
    //                result == ShowResult.Finished
    //                    ? GAEvent.Action.Finished
    //                    : result == ShowResult.Skipped
    //                        ? GAEvent.Action.Skipped
    //                        : GAEvent.Action.Failed)
    //            .SetParameter(zone)
    //            .SetValue(ProfileInfo.Level));
    //}
}
