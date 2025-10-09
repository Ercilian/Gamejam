using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Configuración")]
    public int maxCarryCapacity = 3; // Máximo número de items que puede cargar
    public Transform itemHoldPoint; // Donde aparecen visualmente los items cargados
    public float itemStackOffset = 0.3f; // Separación entre items apilados
    
    [Header("UI")]
    public TextMeshProUGUI itemCountText; // UI que muestra "Madera: 2/3"
    public Image itemIcon;
    
    private List<CollectibleData> carriedItems = new List<CollectibleData>();
    private List<GameObject> visualItems = new List<GameObject>(); // Items visuales en la espalda

    void Start()
    {
        UpdateUI();
    }

    public bool CanCarryItem(CollectibleData item)
    {
        // Si no llevas nada, puedes recoger cualquier tipo
        if (carriedItems.Count == 0) return true;
        // Si llevas algo, solo puedes recoger del mismo tipo y no superar la capacidad máxima
        return carriedItems.Count < maxCarryCapacity && item.type == carriedItems[0].type;
    }

    public void PickupItem(CollectibleData item)
    {
        if (!CanCarryItem(item)) return;
        
        carriedItems.Add(item);
        CreateVisualItem(item);
        UpdateUI();
        
        Debug.Log($"[PlayerInventory] Recogido {item.itemName}. Total: {carriedItems.Count}/{maxCarryCapacity}");
    }

    void CreateVisualItem(CollectibleData item)
    {
        if (!item.itemPrefab || !itemHoldPoint) return;
        
        GameObject visualItem = Instantiate(item.itemPrefab, itemHoldPoint);
        
        // Posicionar el item en la "pila"
        Vector3 stackPosition = Vector3.up * (visualItems.Count * itemStackOffset);
        visualItem.transform.localPosition = stackPosition;
        
        // Desactivar colisiones/triggers del item visual
        Collider itemCollider = visualItem.GetComponent<Collider>();
        if (itemCollider) itemCollider.enabled = false;
        
        // Remover scripts que no necesitamos en el item visual
        WorldCollectible worldScript = visualItem.GetComponent<WorldCollectible>();
        if (worldScript) Destroy(worldScript);
        
        visualItems.Add(visualItem);
    }

    public bool DepositItems(CarFuelSystem carFuelSystem)
    {
        if (carriedItems.Count == 0) return false;

        int totalDieselValue = 0;
        // Recorre de atrás hacia adelante para eliminar correctamente
        for (int i = carriedItems.Count - 1; i >= 0; i--)
        {
            if (carriedItems[i].type == CollectibleData.ItemType.Diesel)
            {
                totalDieselValue += carriedItems[i].dieselValue;
                // Elimina el objeto visual correspondiente
                if (visualItems.Count > i && visualItems[i] != null)
                    Destroy(visualItems[i]);
                if (visualItems.Count > i)
                    visualItems.RemoveAt(i);
                carriedItems.RemoveAt(i);
            }
        }

        if (totalDieselValue > 0)
        {
            carFuelSystem.AddDiesel(totalDieselValue);
            UpdateUI();
            return true;
        }
        return false;
    }

    // Método genérico para depositar por tipo
    private bool DepositItemsByType(CollectibleData.ItemType itemType, System.Action<int> onDeposit)
    {
        if (carriedItems.Count == 0) return false;

        int totalValue = 0;
        for (int i = carriedItems.Count - 1; i >= 0; i--)
        {
            if (carriedItems[i].type == itemType)
            {
                totalValue += GetItemValue(carriedItems[i]);
                // Elimina visual y item
                if (visualItems.Count > i && visualItems[i] != null)
                    Destroy(visualItems[i]);
                if (visualItems.Count > i)
                    visualItems.RemoveAt(i);
                carriedItems.RemoveAt(i);
            }
        }

        if (totalValue > 0)
        {
            onDeposit(totalValue);
            UpdateUI();
            return true;
        }
        return false;
    }

    private int GetItemValue(CollectibleData item)
    {
        return item.type switch
        {
            CollectibleData.ItemType.Diesel => item.dieselValue,
            CollectibleData.ItemType.Scrap => item.scrapValue,
            CollectibleData.ItemType.Moss => item.mossValue,
            _ => 0
        };
    }

    // Métodos públicos específicos para cada sistema
    public bool DepositDieselItems(CarFuelSystem carFuelSystem)
    {
        return DepositItemsByType(CollectibleData.ItemType.Diesel, 
            value => carFuelSystem.AddDiesel(value));
    }

    public bool DepositScrapItems(CarScrapSystem carScrapSystem)
    {
        return DepositItemsByType(CollectibleData.ItemType.Scrap, 
            value => carScrapSystem.AddScrap(value));
    }

    void ClearInventory()
    {
        carriedItems.Clear();
        
        // Destruir items visuales
        foreach (var visualItem in visualItems)
        {
            if (visualItem) Destroy(visualItem);
        }
        visualItems.Clear();
        
        UpdateUI();
    }

    void UpdateUI()
    {
        if (itemCountText)
        {
            if (carriedItems.Count > 0)
            {
                itemCountText.text = $"{carriedItems[0].itemName}: {carriedItems.Count}/{maxCarryCapacity}";
                if (itemIcon && carriedItems[0].itemIcon) 
                    itemIcon.sprite = carriedItems[0].itemIcon;
            }
            else
            {
                itemCountText.text = "";
                if (itemIcon) itemIcon.sprite = null;
            }
        }
    }

    public int GetCarriedItemCount() => carriedItems.Count;
    public bool HasItems() => carriedItems.Count > 0;

    public bool HasScrapItems()
    {
        foreach (var item in carriedItems)
        {
            if (item != null && item.type == CollectibleData.ItemType.Scrap)
            {
                return true;
            }
        }
        return false;
    }

    public int GetCarriedScrapCount()
    {
        int count = 0;
        foreach (var item in carriedItems)
        {
            if (item != null && item.type == CollectibleData.ItemType.Scrap)
            {
                count++;
            }
        }
        return count;
    }
}