using UnityEngine;

using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Setup")]
    public List<UpgradeItemSO> availableUpgrades;
    public ShopItemSlot shopItemSlotPrefab;
    public Transform slotsParent;
    public int slotsCount = 3;

    void Start()
    {
        GenerateShop();
    }

    void GenerateShop()
    {
        List<UpgradeItemSO> pool = new List<UpgradeItemSO>(availableUpgrades);
        for (int i = 0; i < slotsCount && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            UpgradeItemSO upgrade = pool[idx];
            pool.RemoveAt(idx);

            ShopItemSlot slot = Instantiate(shopItemSlotPrefab, slotsParent);
            slot.SetupSlot(upgrade);

            slot.transform.localPosition = new Vector3(i * 2.0f, 0, 0); // 2.0f is the spacing between slots
        }
    }
}
