using UnityEngine;
using System.Collections;
using System;

public class StateActiveChangeNotifier : MonoBehaviour {

    public event Action<bool> OnActiveStateChanged = delegate(bool b) {};

    void OnEnable () {
        OnActiveStateChanged (true);
    }

    void OnDisable () {
        OnActiveStateChanged (false);
    }

}
