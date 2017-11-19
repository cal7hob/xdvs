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
            Debug.LogError("Can't get advice");
            return "";
        }

        return Localizer.GetText(string.Format("advice_{0}", Instance.advices.GetRandomItem()));
    }

}
