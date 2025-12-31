using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeItems", menuName = "Game/Shop/UpgradeItems")]
public class UpgradeItems : ScriptableObject
{

    public string itemName;
    public Sprite itemIcon;
    public string description;
    public int cost;
    public int healthModifier;
    public int damageModifier;
    public int speedModifier;


}
