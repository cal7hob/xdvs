using UnityEngine;

public class MapBonus : MonoBehaviour
{
	public GameObject appearanceEffect;
	private BonusItem bonusItem;

	public void Show()
	{
		bonusItem = GetComponent<BonusItem>();
		bonusItem.SetVisible(false);
        if (appearanceEffect != null)
        {
            appearanceEffect = (GameObject)Instantiate(appearanceEffect, transform.position, Quaternion.identity);
        }
		this.InvokeRepeating(CheckAppearance, 0, 0.15f);
	}

	private void CheckAppearance()
	{
        if (bonusItem.info.appearanceTime > PhotonNetwork.time)
        {
            return;
        }

        if (appearanceEffect != null)
        {
            Destroy(appearanceEffect);
        }
		bonusItem.SetVisible(true);
		this.CancelInvoke(CheckAppearance);
	}

    void OnDestroy()
    {
        if (appearanceEffect != null)
        {
            Destroy(appearanceEffect);
        }
    }
}
