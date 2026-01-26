using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeItem", menuName = "Game/Shop/UpgradeItem")]
public class UpgradeItemSO : ScriptableObject
{

    public string itemName;
    public Sprite icon;
    public string description;
    public int price;
    public int healthModifier;
    public int damageModifier;
    public int speedModifier;


}
