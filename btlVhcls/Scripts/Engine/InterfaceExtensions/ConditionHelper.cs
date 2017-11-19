using UnityEngine;
using System.Collections;
using System;

namespace InterfaceExtensions
{
    [Serializable]
    public class ElementStateData
    {
        public bool colorEnabled = false;
        public Color color = Color.white;
        public bool textEnabled = false;
        public string text = "";
        public bool localPositionEnabled = false;
        public Vector3 localPosition = Vector3.zero;
        public bool sizeEnabled = false;
        public Vector3 size = Vector3.zero;
    }

    [Serializable]
    public class ElementState
    {
        public InterfaceElementBase element;
        public ElementStateData stateData;
    }

    [Serializable]
    public class ElementStateArray
    {
        public string description = "";
        public ElementState[] elementsArray;
    }

    public class ConditionHelper: MonoBehaviour
    {
        int state = 0;

        public ElementStateArray[] states;

        public int State
        {
            get { return state; }
            set
            {
                if (states == null || value > (states.Length - 1))
                    return;
                state = value;

                if(states[value] != null && states[value].elementsArray != null)
                {
                    for(int i = 0; i < states[value].elementsArray.Length; i++)
                    {
                        if(states[value].elementsArray[i] != null && states[value].elementsArray[i].element != null)
                        {
                            if(states[value].elementsArray[i].stateData.textEnabled)
                                states[value].elementsArray[i].element.SetText(states[value].elementsArray[i].stateData.text);
                            if (states[value].elementsArray[i].stateData.colorEnabled)
                                states[value].elementsArray[i].element.SetColor(states[value].elementsArray[i].stateData.color);
                            if (states[value].elementsArray[i].stateData.localPositionEnabled)
                                states[value].elementsArray[i].element.transform.localPosition = states[value].elementsArray[i].stateData.localPosition;
                            if (states[value].elementsArray[i].stateData.sizeEnabled)
                                states[value].elementsArray[i].element.SetSize(states[value].elementsArray[i].stateData.size);
                        }
                    }
                }
            }
        }

        public string StateString
        {
            get
            {
                return (states[state] != null && !string.IsNullOrEmpty(states[state].description)) ?
                    states[state].description :
                    state.ToString();
            }
        }

    }
}
