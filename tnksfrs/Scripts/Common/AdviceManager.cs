using UnityEngine;
using System.Collections.Generic;

public  class AdviceManager: MonoBehaviour
{
    public static AdviceManager Instance { get; private set; }
    public List<int> advices;

    private void Awake()
    {
        Instance = this;
	}

    private void Start()
    {
        transform.parent = GameData.instance.transform;
    }


    private void OnDestroy()
    {
        Instance = null;
    }

    public static string GetRandomAdvice()
    {
        if (Instance == null || Instance.advices == null || Instance.advices.Count == 0)
        {
            DT.LogError("Cant get advice, some var not defined. instance = {0},advices = {1}, advices.Count = {2},",
                Instance == null ? "NULL" : "AdviceManager", Instance.advices == null ? "NULL" : "not null", Instance.advices.Count);
            return "";
        }

        return Localizer.GetText(string.Format("advice_{0}", Instance.advices.GetRandomItem()));
    }

}
