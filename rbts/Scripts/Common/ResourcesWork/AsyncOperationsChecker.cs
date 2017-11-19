using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsyncOperationsChecker : MonoBehaviour
{
    private class OperationDone
    {
        public OperationDone(AsyncOperation operation, Action<AsyncOperation> doneCallback)
        {
            this.operation = operation;
            this.doneCallback = doneCallback;
        }

        public AsyncOperation operation;
        public Action<AsyncOperation> doneCallback;
    }

    public static AsyncOperationsChecker Instance
    {
        get
        {
            if (instance == null)
            {
                CreateInstance();
            }

            return instance;
        }
    }
    private static AsyncOperationsChecker instance;

    private List<OperationDone> checks = new List<OperationDone>();
    private List<OperationDone> checksToDelete = new List<OperationDone>();

    public static void CreateInstance()
    {
        if (instance != null)
            return;

        new GameObject("AsyncOperationChecker").AddComponent<AsyncOperationsChecker>();
        // Присваивание instance произойдёт в Awake()
    }

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (checks.Count == 0)
            return;

        for (int i = 0; i < checks.Count; ++i)
        {
            OperationDone check = checks[i];
            if (check.operation.isDone)
            {
                if (check.doneCallback != null)
                {
                    check.doneCallback(check.operation);
                    checksToDelete.Add(check);
                }
            }
        }

        if (checksToDelete.Count == 0)
            return;

        for (int i = 0; i < checksToDelete.Count; ++i)
        {
            checks.Remove(checksToDelete[i]);
        }

        checksToDelete.Clear();
    }

    void OnDestroy()
    {
        instance = null;
    }

    public void CheckOperation(AsyncOperation operation, Action<AsyncOperation> doneCallback)
    {
        checks.Add(new OperationDone(operation, doneCallback));
    }
}
