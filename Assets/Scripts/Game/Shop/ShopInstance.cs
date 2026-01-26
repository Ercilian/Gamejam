using UnityEngine;

public class ShopInstance : MonoBehaviour
{
    [Header("Prefab del mapa de la tienda")]
    public GameObject shopMapPrefab;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
                Instantiate(shopMapPrefab);
        }
    }
}
