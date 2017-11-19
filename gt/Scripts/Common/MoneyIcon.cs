using UnityEngine;
using System.Collections;

public class MoneyIcon : MonoBehaviour {

    public GameObject goGoldIcon;
    public GameObject goSilverIcon;

    [SerializeField]
    bool isGold = false;
    public bool IsGold {
        get { return isGold; }
        set
        {
            if (value != isGold)
            {
                isGold = value;
                SetState();
            }
        }
    }

    // Use this for initialization
    void Start () {
        SetState();
    }

    void SetState ()
    {
        if (goGoldIcon != null)
        {
            goGoldIcon.SetActive(isGold);
        }
        if (goSilverIcon != null)
        {
            goSilverIcon.SetActive(!isGold);
        }
    }

}
