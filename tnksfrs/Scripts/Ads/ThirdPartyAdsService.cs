using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Абстрактный класс сервиса стороннего поставщика рекламы.
/// </summary>
public abstract class ThirdPartyAdsService : MonoBehaviour, IAdsService
{
    /// <summary>
    /// Настройки показа рекламы от сторонних сервисов.
    /// </summary>
    public class AdsSettings
    {
        /// <summary>
        /// Инициализация настроек показа рекламы от сторонних сервисов.
        /// </summary>
        /// <param name="showingMode">Режим показа рекламы для данного клиента.</param>
        public AdsSettings(AdsShowingMode showingMode)
        {
            ShowingMode = showingMode;
        }

        /// <summary>
        /// Режим показа рекламы.
        /// </summary>
        public AdsShowingMode ShowingMode { get; private set; }

        /// <summary>
        /// Парсинг настроек рекламы, полученных с сервера.
        /// </summary>
        /// <param name="settingsDict">Словарь с настройками рекламы.</param>
        /// <returns>Возвращает экземпляр класса настроек рекламы.</returns>
        public static AdsSettings Parse(Dictionary<string, object> settingsDict)
        {
            AdsShowingMode showingMode = AdsShowingMode.Nowhere;

            foreach (var showingModeSetting in settingsDict)
            {
                AdsShowingMode parsedShowingMode;

                switch (showingModeSetting.Key)
                {
                    case "beforeBattle":
                        parsedShowingMode = AdsShowingMode.BeforeBattle;
                        break;

                    case "afterBattle":
                        parsedShowingMode = AdsShowingMode.AfterBattle;
                        break;

                    case "onQuit":
                        parsedShowingMode = AdsShowingMode.OnQuit;
                        break;

                    default:
                        parsedShowingMode = AdsShowingMode.Nowhere;
                        break;
                }

                if (Convert.ToBoolean(showingModeSetting.Value))
                    showingMode |= parsedShowingMode;
            }

            return new AdsSettings(showingMode);
        }
    }

    [SerializeField]
    protected string parsingKey;

    [SerializeField]
    protected RuntimePlatform[] supportedPlatforms;

    private readonly Action<bool> defaultCallback = adShowed => { };

    private bool closingCallbackUsed;
    private Action<bool> closingCallback;
    private AdsSettings settings;

    /// <summary>
    /// Название сервиса. Используется как ключ для парсинга настроек показа.
    /// </summary>
    public string ParsingKey
    {
        get { return parsingKey; }
    }

    /// <summary>
    /// Условия показа рекламы от сторонних сервисов.
    /// </summary>
    public virtual AdsSettings Settings
    {
        get { return settings = settings ?? new AdsSettings(AdsShowingMode.Nowhere); }
    }

    /// <summary>
    /// Поддерживается ли сервис текущей платформой.
    /// </summary>
    protected virtual bool IsSupportedOnCurrentPlatform
    {
        get
        {
            foreach (RuntimePlatform supportedPlatform in supportedPlatforms)
                if (Application.platform == supportedPlatform)
                    return true;

            return false;
        }
    }

    /// <summary>
    /// Коллбэк завершения попытыки показа рекламы.
    /// </summary>
    public Action<bool> ClosingCallback
    {
        get
        {
            return !closingCallbackUsed ? closingCallback : defaultCallback;
        }

        set
        {
            closingCallbackUsed = false;

            if (closingCallback != null)
                closingCallback -= MarkClosingCallbackUsing;

            closingCallback = value;
            closingCallback += MarkClosingCallbackUsing;
        }
    }

    /// <summary>
    /// В теле имплементированого метода могут быть вызовы запуска сервиса,
    /// подписка на события и др.
    /// </summary>
    /// <returns>Возвращает true, если сервис успешно инициализирован.</returns>
    public abstract bool Initialize();

    /// <summary>
    /// В теле имплементированого метода могут быть вызовы завершения работы сервиса,
    /// отписка от событий и др.
    /// </summary>
    public abstract void Terminate();

    /// <summary>
    /// Вызов попытки показать пользователю рекламу от стороннего сервиса.
    /// </summary>
    public abstract void Show();

    /// <summary>
    /// Установка условий показа рекламы от сторонних сервисов.
    /// </summary>
    /// <param name="settings">Условия показа рекламы.</param>
    public virtual void Setup(AdsSettings settings)
    {
        this.settings = settings;
    }

    /// <summary>
    /// Отметка о том, что заданный коллбэк завершения попытыки показа рекламы уже исполнялся.
    /// </summary>
    /// <param name="adShowed">Была ли показана реклама.</param>
    private void MarkClosingCallbackUsing(bool adShowed)
    {
        closingCallbackUsed = true;
    }
}
