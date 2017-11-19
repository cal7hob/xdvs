using UnityEngine;
using System.Collections;

public class HangarPage : MonoBehaviour, IInterfaceModule
{
    [SerializeField] private bool assignCameraToAnchors = false;

    public bool IsVisible {
        get { return m_wrapper.gameObject.activeSelf; }
    }

    /// <summary>
    /// Конструктор, вызывается на момент создания объекта.
    /// Обращаться к ангару опасно
    /// </summary>
    protected virtual void Create () {

    }

    /// <summary>
    /// Деструктор объекта
    /// </summary>
    protected virtual void Destroy () {

    }

    /// <summary>
    /// Инициализация объекта. Срабатывает только один раз когда ангар инициализирован
    /// </summary>
    protected virtual void Init () {
    }

    /// <summary>
    /// Окно включено
    /// </summary>
    protected virtual void Show () {

    }

    /// <summary>
    /// Окно скрыто
    /// </summary>
    protected virtual void Hide () {

    }

    /// <summary>
    /// Используется сейчас только для инстанируемых страниц
    /// т.к. для страниц из префаба ангара нет ссылки на объект
    /// </summary>
    /// <param name="en"></param>
    public void SetActive(bool en)
    {
        m_wrapper.gameObject.SetActive(en);
    }

    /// <summary>
    /// Вызывается после инициализации окна и если была загрузка профиля.
    /// </summary>
    protected virtual void ProfileChanged () {

    }

    #region internal realisation
    [SerializeField] protected StateActiveChangeNotifier m_wrapper;

    bool m_isInitiated = false;
    bool m_isHangarInitiated = false;
    bool m_callProfileChanged = false;

    void Awake () {
        if (m_wrapper == null) {
            Transform w = transform.Find ("Wrapper");
            if (w == null) {
                w = transform.Find ("wrapper");
            }
            if (w) {
                m_wrapper = w.GetComponent<StateActiveChangeNotifier> ();
                if (m_wrapper == null) {
                    m_wrapper = w.gameObject.AddComponent<StateActiveChangeNotifier> ();
                }
            }
        }
        if (m_wrapper == null) {
            Debug.LogErrorFormat (gameObject, "Wrapper must be set for proper work!");
        }

        SetActive(false);

        Dispatcher.Subscribe (EventId.AfterHangarInit, Init);
        Dispatcher.Subscribe (EventId.ProfileInfoLoadedFromServer, ProfileLoaded);
        m_wrapper.OnActiveStateChanged += WrapperActiveStateChanged;

        #region Назначение камеры в анкоры
        if(assignCameraToAnchors)
        {
            tk2dCameraAnchor[] anchors = m_wrapper.GetComponentsInChildren<tk2dCameraAnchor>(includeInactive: true);
            if (anchors != null)
                for (int i = 0; i < anchors.Length; i++)
                    if (anchors[i].AnchorCamera == null)
                        anchors[i].AnchorCamera = GameData.CurSceneGuiCamera;
        }
        #endregion

        Create();
    }

    void OnDestroy () {
        Dispatcher.Unsubscribe (EventId.AfterHangarInit, Init);
        Dispatcher.Unsubscribe (EventId.ProfileInfoLoadedFromServer, ProfileLoaded);
        m_wrapper.OnActiveStateChanged -= WrapperActiveStateChanged;

        Destroy ();
    }

    protected virtual void Start () {
        if (HangarController.Instance != null && HangarController.Instance.IsInitialized && !m_isHangarInitiated) {
            Init (EventId.AfterHangarInit, null);
        }
    }

    void Init (EventId id, EventInfo info) {
        if (m_isHangarInitiated) return;

        m_isHangarInitiated = true;
        Init ();
        m_isInitiated = true;
        ProfileChanged ();
        if (m_wrapper.gameObject.activeSelf) {
            Show ();
        }
    }

    void WrapperActiveStateChanged (bool isActive) {
        if (!m_isHangarInitiated) return;

        if (isActive) {
            if (!m_isInitiated) {
                Init ();
                m_isInitiated = true;
            }
            if (m_callProfileChanged) {
                m_callProfileChanged = false;
                ProfileChanged ();
            }
            Show ();
        }
        else {
            Hide ();
        }
    }

    void ProfileLoaded (EventId id, EventInfo info) {
        if (!m_isInitiated) return;

        if (IsVisible) {
            ProfileChanged ();
        }
        else {
            m_callProfileChanged = true;
        }
    }

    #endregion
}
