using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIController : MonoBehaviour
{
	private static GUIController instance;
	private static Dictionary<string, Action<string>> buttonClicks;
	private static Dictionary<string, Action<string, tk2dUIItem>> buttonClicksUIItem;

    public static float halfScreenWidth;

	public static GUIController Instance
	{
		get { return instance; }
	}

	/*	public tk2dUILayout prefab;
	public tk2dUIScrollableArea area;

	public void Test()
	{
		tk2dUILayout obj = Instantiate(prefab) as tk2dUILayout;
		obj.gameObject.name = Random.Range(0, 1000000).ToString();
		area.ContentLayoutContainer.AddLayout(obj, tk2dUILayoutItem.FixedSizeLayoutItem());
	}*/

	void Awake()
	{
		if (instance != null)
		{
			Destroy(this);
			return;
		}

		instance = this;
		buttonClicks = new Dictionary<string, Action<string>>(10);
		buttonClicksUIItem = new Dictionary<string, Action<string, tk2dUIItem>>(10);
        
    }

    void Start()
    {
        halfScreenWidth = (float)HangarController.Instance.Tk2dGuiCamera.nativeResolutionWidth / 2;
    }

	void OnDestroy()
	{
		if (instance != this)
			return;

		instance = null;
		buttonClicks.Clear();
		if (buttonClicks.Count == 0)
			buttonClicks = null;

        buttonClicksUIItem.Clear();
        buttonClicksUIItem = null;
    }

	/*   PRIVATE SECTION   */
	
	private void ButtonClick(tk2dUIItem item)
	{
		Action<string> act;
		buttonClicks.TryGetValue(item.name, out act);
		if (act != null)
			act(item.name);
	}

	private void ButtonClickUIItem(tk2dUIItem item)
	{
		Action<string, tk2dUIItem> act;
		buttonClicksUIItem.TryGetValue(item.name, out act);
		if (act != null)
			act(item.name, item);
	}
	
	/*   PUBLIC SECTION   */
	
	public static void ListenButtonClick(string name, Action<string> handler)
	{
		if (!buttonClicks.ContainsKey(name))
			buttonClicks.Add(name, handler);
		else
			buttonClicks[name] += handler;
	}

    public static void RemoveButtonClickListener (string name, Action<string> handler)
    {
        if (buttonClicks.ContainsKey (name))
            buttonClicks[name] -= handler;
    }

    public static void ListenButtonClickUIItem (string name, Action<string, tk2dUIItem> handler)
	{
		if (!buttonClicksUIItem.ContainsKey(name))
			buttonClicksUIItem.Add(name, handler);
		else
			buttonClicksUIItem[name] += handler;
	}

    public static float ScrollPanel (tk2dUIScrollableArea panel, float scrollValue)
    {
        if (scrollValue < 0)
            return scrollValue;

        if (scrollValue >= 2) {
            scrollValue -= 2;
            panel.Value = scrollValue;
        }
        else
            panel.Value = Mathf.MoveTowards (panel.Value, scrollValue, 0.7f * Time.deltaTime);

        if (HelpTools.Approximately (panel.Value, scrollValue))
            return -1;

        return scrollValue;
    }



	/*public static void ForgetButtonClick(string name, SimpleAction handler)
	{
		if (buttonClicks.ContainsKey(name))
			buttonClicks[name] -= handler;
	}*/


	public static TComponent CheckReferentObject<TComponent>(Transform parentTransform, string path, TComponent field, bool verbose = true)
		where TComponent : Component
	{
		if (field != null)
			return field;

		Transform targetTransform = parentTransform.Find(path);

		if (targetTransform == null)
		{
			if (verbose)
                DT.LogError("\"{0}\" not found!", path);
			return null;
		}

		TComponent resultComponent = targetTransform.gameObject.GetComponent<TComponent>();

		if (resultComponent == null)
			DT.LogError("Cannot find component \"{0}\" in \"{1}\"!", typeof(TComponent).ToString(), path);

		return resultComponent;
	}

	public static GameObject CheckReferentObject(Transform parentTransform, string path, GameObject field)
	{
		if (field != null)
			return field;

		Transform targetTransform = parentTransform.Find(path);

		if (targetTransform == null)
		{
			DT.LogError("\"{0}\" not found!", path);
			return null;
		}

		return targetTransform.gameObject;
	}

	public static List<Transform> CheckReferentObject(Transform parentTransform, string[] paths, List<Transform> field)
	{
		if (field != null)
			return field;

		List<Transform> resultTransforms = new List<Transform>();

		foreach (string path in paths)
		{
			Transform targetTransform = parentTransform.Find(path);

			if (targetTransform == null)
			{
				DT.LogError("\"{0}\" not found!", path);
				continue;
			}

			resultTransforms.Add(targetTransform);
		}

		return resultTransforms;
	}
}
