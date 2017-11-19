using UnityEngine;

public class SimpleItemsPanel : ItemsPanel
{ 
    [SerializeField] private GameObject lotPrefab;

    public T CreateItem<T>() where T : MonoBehaviour
    {
        var lot = CreateLotByGivenPrefab(lotPrefab) as T;

        return lot != null ? lot.GetComponent<T>() : null;
    }
}
