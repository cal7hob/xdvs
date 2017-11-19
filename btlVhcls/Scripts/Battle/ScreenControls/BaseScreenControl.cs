using UnityEngine;
using System.Collections;

namespace XDevs.ScreenControls
{
    public class BaseScreenControl : MonoBehaviour
    {
        public event System.Action<BaseScreenControl, bool> StateChangedAction;

        [Header("Base control setup")]
        [SerializeField] protected tk2dUIItem uiItem;
        [SerializeField] protected tk2dSprite controlSprite;
        [SerializeField] protected float areaExtendQualifier = 0.07f;

        [Header("Sprite colors")]
        [SerializeField] protected Color colorNormalState = Color.white;
        [SerializeField] protected Color colorDisabledState = Color.white;

        public Rect Area { get; private set; }
        public bool IsPressed { get { return uiItem.IsPressed; } }

        public bool IsEnabled {
            get { return m_isEnabled; }
            private set {
                bool doTriggerEvent = m_isEnabled != value;
                m_isEnabled = value;
                if (doTriggerEvent) OnStateChanged();
            }
        }

        public bool IsActive {
            get { return m_isActive && IsEnabled; }
            private set {
                bool doTriggerEvent = IsEnabled && (m_isActive != value);
                m_isActive = value;
                if (doTriggerEvent) OnStateChanged();
            }
        }

        protected virtual void Init()
        {
            Log("Init");
            if (uiItem == null) {
                Debug.LogErrorFormat(this, "{0}: uiItem is null!", name);
                return;
            }
            if (controlSprite == null) {
                Debug.LogErrorFormat(this, "{0}: controlSprite is null!", name);
                return;
            }
            uiItem.OnDown += OnDown;
            uiItem.OnUp += OnUp;
            uiItem.OnClick += OnClick;
            uiItem.OnRelease += OnRelease;

            CalcArea();

            m_isInitialized = true;
        }

        protected void CalcArea()
        {
            Bounds bounds = controlSprite.GetBounds();

            float additionalLengthX = bounds.max.x * areaExtendQualifier;
            float additionalLengthY = bounds.max.y * areaExtendQualifier;

            Vector3 worldTopLeftPosition
                = controlSprite.transform.TransformPoint(
                    new Vector3(
                        x: bounds.min.x,
                        y: bounds.max.y,
                        z: controlSprite.transform.localPosition.z));

            Vector3 worldBottomRightPosition
                = controlSprite.transform.TransformPoint(
                    new Vector3(
                        x: bounds.max.x,
                        y: bounds.min.y,
                        z: controlSprite.transform.localPosition.z));

            Vector3 sreenTopLeftPosition = tk2dCamera.Instance.ScreenCamera.WorldToScreenPoint(worldTopLeftPosition);
            Vector3 sreenBottomRightPosition = tk2dCamera.Instance.ScreenCamera.WorldToScreenPoint(worldBottomRightPosition);

            Area = new Rect
            {
                xMin = sreenTopLeftPosition.x - additionalLengthX,
                yMin = sreenBottomRightPosition.y - additionalLengthY,
                xMax = sreenBottomRightPosition.x + additionalLengthX,
                yMax = sreenTopLeftPosition.y + additionalLengthY
            };
        }

        protected void Activate()
        {
            Log("Activate");
            IsActive = true;
        }

        protected void Deactivate()
        {
            Log("Deactivate");
            IsActive = false;
        }

        protected virtual void Destroyed()
        {
            uiItem.OnDown -= OnDown;
            uiItem.OnUp -= OnUp;
            uiItem.OnClick -= OnClick;
            uiItem.OnRelease -= OnRelease;
            Log("Destroyed");
        }

        protected virtual void Enabled()
        {
            Log("Enabled");
            IsEnabled = true;
        }

        protected virtual void Disabled()
        {
            Log("Disabled");
            IsEnabled = false;
        }

        protected virtual void OnStateChanged ()
        {
            Log("On state changed: enabled={0}, active={1}", IsEnabled, IsActive);
            controlSprite.color = IsActive ? colorNormalState : colorDisabledState;
            if (StateChangedAction != null) StateChangedAction(this, IsActive);
        }

        protected virtual void OnDown ()
        {
            Log("OnDown");
        }
        protected virtual void OnUp()
        {
            Log("OnUp");
        }
        protected virtual void OnClick()
        {
            Log("OnClick");
        }
        protected virtual void OnRelease()
        {
            Log("OnRelease");
        }

        protected virtual void Log(string msg, params object[] parameters)
        {
            if (!m_isDebugEnabled) return;
            Debug.LogFormat(this, name+": " +msg, parameters);
        }

        #region Private Area
        [SerializeField]
        bool m_isDebugEnabled = true;
        bool m_isInitialized = false;
        bool m_isEnabled = false;
        bool m_isActive = true;

        void OnEnable()
        {
            if (!m_isInitialized) Init();
            Enabled();
        }

        void OnDisable()
        {
            Disabled();
        }

        void OnDestroy()
        {
            Destroyed();
        }
        #endregion

    }
}