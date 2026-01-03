using UnityEngine;

using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Setup")]
    public List<UpgradeItemSO> availableUpgrades; // Asigna aquí todos los upgrades posibles
    public ShopItemSlot shopItemSlotPrefab; // Prefab del slot
    public Transform slotsParent; // Donde se instanciarán los slots
    public int slotsCount = 3; // Número de objetos en la tienda

    void Start()
    {
        GenerateShop();
    }

    void GenerateShop()
    {
        // Selecciona upgrades aleatorios (puedes mejorar la lógica si quieres evitar repeticiones)
        List<UpgradeItemSO> pool = new List<UpgradeItemSO>(availableUpgrades);
        for (int i = 0; i < slotsCount && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            UpgradeItemSO upgrade = pool[idx];
            pool.RemoveAt(idx);

            ShopItemSlot slot = Instantiate(shopItemSlotPrefab, slotsParent);
            slot.SetupSlot(upgrade);

            // Alinea los slots horizontalmente (ajusta el offset según el tamaño de tu prefab)
            slot.transform.localPosition = new Vector3(i * 2.0f, 0, 0); // 2.0f es la separación entre slots
        }
    }
}
