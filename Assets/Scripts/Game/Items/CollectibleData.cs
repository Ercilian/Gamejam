using UnityEngine;

[CreateAssetMenu(fileName = "New Collectible", menuName = "Game/Collectible Data")]
public class CollectibleData : ScriptableObject
{
    [Header("Info del Coleccionable")]
    public string itemName;
    public string itemID;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public ItemType type;
    public enum ItemType
    {
        Diesel,
        Scrap,
        Moss
    }
    
    [Header("Propiedades Diesel")]
    public int dieselValue;
    public float collectRange;
    public string collectPrompt = "Presiona E para recoger diesel";

    [Header("Propiedades Scrap")]
    public int scrapValue;
    public float scrapCollectRange;
    public string scrapCollectPrompt = "Presiona E para recoger scrap";

    [Header("Propiedades Moss")]
    public int mossValue;
    public float mossCollectRange;
    public string mossCollectPrompt = "Presiona E para recoger moss";

    [Header("Audio")]
    public AudioClip collectSound;
    public AudioClip depositSound;
}