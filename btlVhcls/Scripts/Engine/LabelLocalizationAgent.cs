using UnityEngine;
using System.Collections;

public class LabelLocalizationAgent : MonoBehaviour
{
    public string key;
    public string Key { get { return !string.IsNullOrEmpty(key) ? key : textMesh.name; } }
    private string locString;
    private string parameter;
    private tk2dTextMesh textMesh;
    private tk2dUITextInput textInput;

    

	public string Parameter
	{
		set
		{
			parameter = value;
			LocalizeLabel();
		}
	}
	
	static public void LocalizeTranformChild (Transform parent, string path, string key = null) {
		if (!parent) {
			return;
		}
		Transform tr = parent.Find (path);
		if (!tr) {
			return;
		}
		if (tr.gameObject.GetComponent<LabelLocalizationAgent> ()) {
			return;
		}
		LabelLocalizationAgent loc = tr.gameObject.AddComponent <LabelLocalizationAgent>() as LabelLocalizationAgent;
		if (!string.IsNullOrEmpty (key)) {
			loc.key = key;
		}
		loc.LocalizeLabel ();
	}
	
	protected virtual void Awake()
	{
		textMesh = GetComponent<tk2dTextMesh>();
		textInput = GetComponent<tk2dUITextInput>();
		if (!textMesh && !textInput)
		{
			Destroy(this);
			return;
		}
		
		Localizer.AddLabelAgent(this);
		if (Localizer.Loaded)
			LocalizeLabel();
	}

	protected tk2dFontData GetFont () {
		if (textMesh)
			return textMesh.font;
		if (textInput)
			return textInput.inputLabel.font;
		return null;
	}

	protected virtual string GetLocalizedString () {
		return Localizer.GetText(string.IsNullOrEmpty(key) ? name : key, parameter);
	}

	private void OnDestroy()
	{
		Localizer.RemoveLabelAgent(this);
	}

	public void LocalizeLabel()
	{
		locString = GetLocalizedString ();
		if (textMesh)
        {
            textMesh.text = locString;
            textMesh.Commit();
        }
		if (textInput)
			textInput.emptyDisplayText = locString;
	}
}
