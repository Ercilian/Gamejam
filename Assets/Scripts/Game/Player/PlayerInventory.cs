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
        return carriedItems.Count < maxCarryCapacity;
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

    public bool DepositItems(CarFuelSystem carFuelSystem) // Cambio: CarController -> CarFuelSystem
    {
        if (carriedItems.Count == 0) return false;
        
        int totalDieselValue = 0;
        
        // Calcular valor total
        foreach (var item in carriedItems)
        {
            totalDieselValue += item.dieselValue;
        }
        
        // Depositar en el carro
        carFuelSystem.AddDiesel(totalDieselValue);
        
        // Limpiar inventario
        ClearInventory();
        
        Debug.Log($"[PlayerInventory] Depositados {carriedItems.Count} items por {totalDieselValue} diesel");
        
        return true;
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
}