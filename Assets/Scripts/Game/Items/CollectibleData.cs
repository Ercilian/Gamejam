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
        Gold,
        Moss
    }
    
    [Header("Propiedades")]
    public int dieselValue = 10; // Cuánto diesel da al carro
    public float collectRange = 2f; // Distancia para recogerlo
    public string collectPrompt = "Presiona E para recoger diesel";

    [Header("Audio")]
    public AudioClip collectSound;
    public AudioClip depositSound;
}