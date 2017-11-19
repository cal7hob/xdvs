using UnityEngine;
using System.Collections;

public class HangarPage : MonoBehaviour, IInterfaceModule
{
    [SerializeField] private bool stateOnCreating = false;//включает / выключает врапер при создании
    [SerializeField] protected bool exitToMainMenuOnMessageBoxAppears = false;
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

    public void SetActive(bool en)
    {
        m_wrapper.gameObject.SetActive(en);
    }

    /// <summary>
    /// Вызывается после инициализации окна и если была загрузка профиля.
    /// </summary>
    protected virtual void ProfileChanged () {

    }

    /// <summary>
    /// Часто требуется закрывать окно при открытии месседжбокса, если в окне есть прокрутка с масками
    /// чтобы элементы прокручиваемой панели не перекрывали месседжбокс
    /// </summary>
    protected virtual void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        EventInfo_B eInfo = (EventInfo_B)info;
        if (exitToMainMenuOnMessageBoxAppears && IsVisible && eInfo.bool1)
            GUIPager.ToMainMenu();
    }

    #region internal realisation
    [SerializeField] protected StateEventSender m_wrapper;

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
                m_wrapper = w.GetComponent<StateEventSender> ();
                if (m_wrapper == null) {
                    m_wrapper = w.gameObject.AddComponent<StateEventSender> ();
                }
            }
        }
        if (m_wrapper == null) {
            Debug.LogErrorFormat (gameObject, "Wrapper must be set for proper work!");
        }

        Messenger.Subscribe (EventId.AfterHangarInit, Init);
        Messenger.Subscribe (EventId.ProfileInfoLoadedFromServer, ProfileLoaded);
        Messenger.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        m_wrapper.StateChanged += WrapperActiveStateChanged;

        m_wrapper.gameObject.SetActive(stateOnCreating);

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
        Messenger.Unsubscribe (EventId.AfterHangarInit, Init);
        Messenger.Unsubscribe (EventId.ProfileInfoLoadedFromServer, ProfileLoaded);
        Messenger.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        m_wrapper.StateChanged -= WrapperActiveStateChanged;

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

    void WrapperActiveStateChanged (StateEventSender stanEventSender, bool isActive) {
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
