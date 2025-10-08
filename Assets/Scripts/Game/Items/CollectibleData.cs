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
    public int dieselValue; // Cuánto diesel da al carro
    public float collectRange; // Distancia para recogerlo
    public string collectPrompt = "Presiona E para recoger diesel";

    [Header("Propiedades Scrap")]
    public int scrapValue; // Cuánto scrap da al carro
    public float scrapCollectRange; // Distancia para recogerlo
    public string scrapCollectPrompt = "Presiona E para recoger scrap";

    [Header("Propiedades Moss")]
    public int mossValue; // Cuánto moss da al carro
    public float mossCollectRange; // Distancia para recogerlo
    public string mossCollectPrompt = "Presiona E para recoger moss";

    [Header("Audio")]
    public AudioClip collectSound;
    public AudioClip depositSound;
}