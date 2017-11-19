using UnityEngine;
using System;
using System.Collections.Generic;

public class ActivationScript : MonoBehaviour, IActivated
{
    [Serializable]//fucking unity serialization
    public class BoolStates
    {
        public GameObject go;
        public bool activeState;
        public bool inactiveState;
    }
    [Serializable]
    public class ColorStates
    {
        public GameObject go;
        public Color activeColor;
        public Color inactiveColor;

        public void SetColor(bool isActivated)
        {
            if (go == null)
            {
                Debug.LogErrorFormat("Not assigned element in 'objectsToChangeAlpha' array!");
                return;
            }

            Color c = isActivated ? activeColor : inactiveColor;

            tk2dBaseSprite sprite = go.GetComponent<tk2dBaseSprite>();
            if (sprite)
                sprite.color = c;

            tk2dTextMesh lbl = go.GetComponent<tk2dTextMesh>();
            if (lbl)
                lbl.color = c;
        }
    }

    [SerializeField] private BoolStates[] objectsToChangeActivity;
    [SerializeField] private ColorStates[] objectsToChangeColor;
    

    private bool isInited = false;
    protected bool isActivated = true;

    void Awake()
    {
        isInited = true;
        UseActivation();
    }


    public bool Activated
    {
        get { return isActivated; }
        set
        {
            if (isInited && isActivated == value)
                return;

            isActivated = value;

            if (isInited)
                UseActivation();
        }
    }

    protected virtual void UseActivation()
    {
        if (objectsToChangeActivity != null)
            for (int i = 0; i < objectsToChangeActivity.Length; i++)
                if (objectsToChangeActivity[i].go)
                    objectsToChangeActivity[i].go.SetActive(isActivated ? objectsToChangeActivity[i].activeState : objectsToChangeActivity[i].inactiveState);

        if (objectsToChangeColor != null)
            for (int i = 0; i < objectsToChangeColor.Length; i++)
                objectsToChangeColor[i].SetColor(isActivated);
    }
}
