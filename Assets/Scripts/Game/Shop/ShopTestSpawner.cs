using UnityEngine;

public class ShopTestSpawner : MonoBehaviour
{
    public ShopItemSlot slot;
    public UpgradeItemSO testUpgrade;

    void Start()
    {
        if (slot != null && testUpgrade != null)
        {
            slot.SetupSlot(testUpgrade);
        }
    }
}
