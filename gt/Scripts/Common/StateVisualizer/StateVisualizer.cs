using System;
using UnityEngine;

[Serializable]
public class VisualState<T>
{
    public T[] states;
    public GameObject[] gameObjectsToSetActive;
}

public abstract class StateVisualizer : MonoBehaviour
{
    // <summary>
    // Enable all GameObjects binded to state
    // </summary>
    // <param name="state">State to be activated</param>
    // <param name="States">States to seek from</param>
    public void SetState<T>(T state, VisualState<T>[] States)
    {
        //this.state = state;
        bool haveStates = true;

        foreach (VisualState<T> visualState in States)
        {
            if (visualState != null)
            {
                haveStates = true;

                foreach (T stateToCheck in visualState.states)
                {
                    if (NotEqual(And(state, stateToCheck), stateToCheck))
                    {
                        haveStates = false;
                        break;
                    }
                }

                foreach (GameObject gameObject in visualState.gameObjectsToSetActive)
                {
                    //if (haveStates == true)
                    //    Debug.LogError("Setting " + gameObject + " active");
                    
                    gameObject.SetActive(haveStates);
                }
            }
        }
    }

    static T And<T>(T a, T b)
    {
        // consider adding argument validation here

        if (Enum.GetUnderlyingType(a.GetType()) != typeof(ulong))
            return (T)Enum.ToObject(a.GetType(), Convert.ToInt64(a) & Convert.ToInt64(b));
        else
            return (T)Enum.ToObject(a.GetType(), Convert.ToUInt64(a) & Convert.ToUInt64(b));
    }

    static bool NotEqual<T>(T a, T b)
    {
        // consider adding argument validation here

        if (Enum.GetUnderlyingType(a.GetType()) != typeof(ulong))
            return Convert.ToInt64(a) != Convert.ToInt64(b);
        else
            return Convert.ToUInt64(a) != Convert.ToUInt64(b);
    }
}
