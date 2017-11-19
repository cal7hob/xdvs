using UnityEngine;
using System;
using System.Collections.Generic;
using Rewired;

namespace XDevs.ScreenControls
{

    public class ScreenController : MonoBehaviour
    {

        [Serializable]
        protected class Button
        {
            [RewiredCustomButton("Assets/2D/GUIPrefabs/Rewired Input Manager.prefab", "TouchControl")]
            public int rewiredButtonId;
            public BaseScreenControl guiButton;
        }

        [SerializeField] protected List<Button> buttons;
        private CustomController touchController;

        bool m_isItialized = false;

        void Init ()
        {
            touchController = XDevs.Input.TouchController;
            m_isItialized = true;
        }

        void OnEnable()
        {
            if (!m_isItialized) Init();
            ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
        }

        void OnDisable()
        {
            ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
        }

        private void ReInput_InputSourceUpdateEvent()
        {
            foreach (var b in buttons) {
                touchController.SetButtonValueById(b.rewiredButtonId, false/*b.guiButton.IsPressed*/);
            }
            //touchController.SetAxisValue(horizontalAxisKey, GetXAxis());
            //touchController.SetAxisValue(verticalAxisKey, GetYAxis());
        }
    }

}