using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс для работы с рекламой от сторонних сервисов.
/// </summary>
public class ThirdPartyAdsManager : MonoBehaviour
{
    private event Action Closing = delegate { };

    [SerializeField]
    internal ThirdPartyAdsService[] services;

    private static ThirdPartyAdsManager instance;

    private List<ThirdPartyAdsService> showingServices;

    public static bool IsShowing { get; private set; }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        List<ThirdPartyAdsService> initializedServices = new List<ThirdPartyAdsService>();

        foreach (ThirdPartyAdsService service in services)
            if (service.Initialize())
                initializedServices.Add(service);
                          
        services = initializedServices.ToArray();
    }

    void OnDestroy()
    {
        if (instance != this || services == null)
            return;

        foreach (var service in services)
            service.Terminate();

        Closing = null;
    }

    /// <summary>
    /// Передача настроек показа реклама для парсинга.
    /// </summary>
    /// <param name="serviceName">Название сервиса.</param>
    /// <param name="settingsDict">Словарь с настройками рекламы.</param>
    public static void SetupService(string serviceName, Dictionary<string, object> settingsDict)
    {
        if (settingsDict.ContainsKey("freeAdsDays"))
            GameData.adsFreeDaysQuantity = Convert.ToInt32(settingsDict["freeAdsDays"]);

        foreach (ThirdPartyAdsService service in instance.services)
        {
            if (service.ParsingKey != serviceName)
                continue;

            service.Setup(ThirdPartyAdsService.AdsSettings.Parse(settingsDict));

            return;
        }

        Debug.LogWarningFormat(
            "All referenced ThirdPartyAdsService doesn't match received settings key \"{0}\"! "
                + "Try to assign proper parsing key for suitable ThirdPartyAdsService in ThirdPartyAdsManager "
                + "or check incoming data.",
            serviceName);
    }

    /// <summary>
    /// Показ рекламы от сторонних сервисов.
    /// </summary>
    /// <param name="targetPlace">Место вызова рекламы
    /// (параметр проверяется на соответствие установленным AdsSettings).</param>
    /// <param name="closingCallback">Коллбэк закрытия окна рекламы.</param>
    public static void Show(AdsShowingMode targetPlace, Action closingCallback)
    {
        IsShowing = true;

        instance.Closing = delegate {};
        instance.Closing += closingCallback;
        instance.Closing += OnClosing;
        
        if (!XD.StaticContainer.Profile.BattleTutorialCompleted)
        {
            instance.Closing();
            return;
        }

        instance.showingServices = new List<ThirdPartyAdsService>();

        foreach (var service in instance.services)
        {
            if ((service.Settings.ShowingMode & targetPlace) == AdsShowingMode.Nowhere)
                continue;

            service.ClosingCallback = instance.ShowingCallback;

            instance.showingServices.Add(service);
        }

        if (instance.showingServices.Count > 0)
        {
            instance.showingServices[0].Show();
            return;
        }

        instance.Closing();
    }

    /// <summary>
    /// Коллбэк показа рекламы от стороннего сервиса.
    /// </summary>
    /// <param name="success">Успешность запуска показа.</param>
    private void ShowingCallback(bool success)
    {
        if (success)
        {
            Closing();
            return;
        }

        if (showingServices.Count > 1)
        {
            showingServices.RemoveAt(0);
            showingServices[0].Show();

            return;
        }

        Closing();
    }

    private static void OnClosing()
    {
        IsShowing = false;
    }
}
