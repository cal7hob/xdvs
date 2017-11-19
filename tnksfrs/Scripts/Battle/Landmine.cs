using UnityEngine;
using XDevs.LiteralKeys;
using System.Collections.Generic;
using XD;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class Landmine : MonoBehaviour
{
    private static List<int> myLandmineIds = new List<int>(3);

    public GameObject explosionEffect;

    private static int playerLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Player]);
	private PhotonView photonView;
    private static int placeMask = MiscTools.GetLayerMask(Layer.Key.Terrain);

	void OnPhotonInstantiate(PhotonMessageInfo messageInfo)
	{
		photonView = GetComponent<PhotonView>();
		PutCorrectly();
    }

	void OnTriggerEnter(Collider other)
	{
        if (!PhotonNetwork.isMasterClient || other.GetComponentInParent<VehicleController>() == null) //Активируется любым Vehicle'ом на клиенте мастера
			return;

		EffectPoolDispatcher.GetFromPool(explosionEffect, transform.position, Quaternion.identity);
        VehicleController vehicleController = StaticContainer.BattleController.CurrentUnit;

		int damage = (int)Random.Range(vehicleController.MaxArmor * 0.4f, vehicleController.MaxArmor * 0.8f);

        vehicleController.HPSystem.ChangeHitPoints(damage, -1, true);

		vehicleController.Player.SetCustomProperties(new Hashtable() { { "hl", vehicleController.HPSystem.Armor } });

	    Dispatcher.Send(
	        id:     EventId.TankTakesDamage,
	        info:   new EventInfo_U(
                        StaticType.BattleController.Instance<IBattleController>().MyPlayerId,
                        damage,
                        photonView.ownerId,
                        (int)GunShellInfo.ShellType.Usual,
                        transform.position), // Временнно, пока тип мины как снаряда не определён.
	        target: Dispatcher.EventTargetType.ToAll);
		
        PhotonNetwork.Destroy(gameObject);
	}
    
	private void PutCorrectly()
	{
		RaycastHit hit;

	    if (!Physics.Raycast(transform.position, Vector3.down, out hit, 10, placeMask))
	    {
	        return;
	    }

		Vector3 upper = hit.normal;
		transform.position = hit.point + upper * (GetComponent<Collider>() as BoxCollider).size.y * transform.lossyScale.y / 2;
		transform.up = upper;
	}
}