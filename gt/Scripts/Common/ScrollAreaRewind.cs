using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollAreaRewind : MonoBehaviour
{
    public tk2dUIScrollableArea area;
    public float speed = 1;
    public float delay = 0;
    public bool ActivateOnEnable;
    private bool activateUpdate;
    private bool started;
    void Start()
    {
        Invoke("Activate", delay);
    }

    void OnEnable()
    {
        if (!started) return;
        if (!ActivateOnEnable) return;
        Start();
    }

    void Activate()
    {
        area.Value = 1;
        activateUpdate = true;
        started = true;
    }

    void Update()
    {
        if (!activateUpdate) return;
        area.Value -= Time.deltaTime * speed;
        if (area.Value <= 0)
        {
            activateUpdate = false;
        }
    }
}
