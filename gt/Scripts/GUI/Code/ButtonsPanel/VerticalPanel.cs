using UnityEngine;
using System.Collections.Generic;

namespace XDevs.ButtonsPanel
{

    public class VerticalPanel : MonoBehaviour
    {
        protected enum AlignType
        {
            Top,
            Center,
            Bottom
        }

        public float startYPos = 0;
        public float spaceBetweenButtons = 10f;
        [SerializeField]
        protected AlignType alignBy = AlignType.Top;

        public List<PanelButton> buttons = new List<PanelButton>();

        public int GetActiveButtonsCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < buttons.Count; i++)
                    if (buttons[i].gameObject.activeSelf)
                        n++;
                return n;
            }
        }

        public void AddButton(PanelButton button, int index = -1)
        {
            if (buttons.Contains (button))
            {
                return;
            }

            Vector3 pos = button.transform.localPosition;
            button.transform.parent = transform;
            button.transform.localPosition = pos;
            buttons.Insert( index == -1 ? buttons.Count : (int)Mathf.Round(Mathf.Clamp((float)index, 0f, (float)buttons.Count)), button);
            button.StateChanged += OnButtonStateChanged;
            Align();
        }





        protected bool doAlignOnEnable = false;

        virtual protected void Start()
        {
            foreach (var btn in buttons)
            {
                btn.StateChanged += OnButtonStateChanged;
            }
            Align();
        }

        virtual protected void OnEnable ()
        {
            if (doAlignOnEnable)
            {
                Align();
                doAlignOnEnable = false;
            }
        }


        virtual public void Align ()
        {
            if (!isActiveAndEnabled)
            {
                doAlignOnEnable = true;
                return;
            }
            if (buttons == null || buttons.Count == 0)
            {
                return;
            }

            switch (alignBy)
            {
                case AlignType.Top:
                    AlignByTop ();
                    break;
                case AlignType.Center:
                    AlignByCenter();
                    break;
                case AlignType.Bottom:
                    AlignByBottom();
                    break;
            }
        }

        protected void AlignBySide(AlignType alignType)
        {
            if (alignType == AlignType.Center)
                return;
            int sign = alignType == AlignType.Top ? -1 : 1;

            float pos = startYPos;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    var b = buttons[i];
                    if (!b.isActiveAndEnabled)
                    {
                        continue;
                    }
                    var t = b.transform;
                    t.localPosition = new Vector3(t.localPosition.x, pos);
                    pos = pos + sign*(b.height + spaceBetweenButtons);
                }
            }
        }

        virtual protected void AlignByTop()
        {
            AlignBySide(AlignType.Top);
        }

        virtual protected void AlignByBottom()
        {
            AlignBySide(AlignType.Bottom);
        }

        virtual protected void AlignByCenter()
        {
            float height = startYPos;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    var b = buttons[i];
                    if (!b.isActiveAndEnabled)
                    {
                        continue;
                    }
                    height += b.height + spaceBetweenButtons;
                }
            }
            float pos = (height / 2.0f) + startYPos - spaceBetweenButtons;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    var b = buttons[i];
                    if (!b.isActiveAndEnabled)
                    {
                        continue;
                    }
                    var t = b.transform;
                    t.localPosition = new Vector3(t.localPosition.x, pos);
                    pos -= b.height + spaceBetweenButtons;
                }
            }
        }

        void OnButtonStateChanged(StateEventSender btn, bool state)
        {
            Align();
        }

    }

}
