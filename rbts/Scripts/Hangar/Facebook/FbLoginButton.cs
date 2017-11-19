using UnityEngine;
using System.Collections;

public class FbLoginButton : MonoBehaviour {

	public GameObject	activateGO;
	public GameObject	loginGO;
    public tk2dTextMesh lFbRewardMoney;
    public tk2dSlicedSprite moneyIcon;

	public bool isActivateMode {
		get {
			return m_isActivate;
		}
		set {
			m_isActivate = value;
			ApplyMode ();
		}
	}

	[SerializeField]
	private bool m_isActivate;

    void Awake()
    {
        if (lFbRewardMoney == null)
            DT.LogError ("lFbRewardMoney is null!");
        if (moneyIcon == null)
            DT.LogError ("moneyIcon is null");
    }

	// Use this for initialization
	void Start () {
		ApplyMode ();
	}

	void OnEnable () {
		ApplyMode ();
	}

	private void ApplyMode () {
		if (activateGO != null) activateGO.SetActive (m_isActivate);
		if (loginGO != null)	loginGO.SetActive (!m_isActivate);
	}
}
