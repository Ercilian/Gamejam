using UnityEngine;
using System.Collections.Generic;

public class ItemDropSystem : MonoBehaviour
{
    [System.Serializable]
    public class DroppableItem
    {
        public string itemName;
        public GameObject prefab;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    [Header("Item Prefabs")]
    public DroppableItem[] availableItems;

    [System.Serializable]
    public class DropTable
    {
        public string sourceName;

        [System.Serializable]
        public class ItemDrop
        {
            public int itemIndex;
            [Range(0f, 100f)]
            public float dropChance;
        }

        public ItemDrop[] possibleDrops;
    }

    [SerializeField]
    [Header("Drop Tables")]
    private DropTable[] dropTables = {
        new DropTable {
            sourceName = "NormalEnemy",
            possibleDrops = new DropTable.ItemDrop[] {
                new DropTable.ItemDrop { itemIndex = 0, dropChance = 60f },
                new DropTable.ItemDrop { itemIndex = 1, dropChance = 15f },
                new DropTable.ItemDrop { itemIndex = 2, dropChance = 5f }  
            }
        },
        new DropTable {
            sourceName = "EliteEnemy",
            possibleDrops = new DropTable.ItemDrop[] {
                new DropTable.ItemDrop { itemIndex = 0, dropChance = 80f },
                new DropTable.ItemDrop { itemIndex = 1, dropChance = 40f },
                new DropTable.ItemDrop { itemIndex = 2, dropChance = 20f } 
            }
        },
        new DropTable {
            sourceName = "DestructibleObjectDiesel",
            possibleDrops = new DropTable.ItemDrop[] {
                new DropTable.ItemDrop { itemIndex = 0, dropChance = 30f },
                new DropTable.ItemDrop { itemIndex = 1, dropChance = 10f } 
            }
        },
        new DropTable {
            sourceName = "DestructibleObjectPlant",
            possibleDrops = new DropTable.ItemDrop[] {
                new DropTable.ItemDrop { itemIndex = 0, dropChance = 50f }, 
                new DropTable.ItemDrop { itemIndex = 1, dropChance = 20f } 
            }
        }
    };

    [Header("Drop Quantity Probabilities")]
    [Range(0f, 1f)] public float probDrop1 = 0.65f;
    [Range(0f, 1f)] public float probDrop2 = 0.25f;
    [Range(0f, 1f)] public float probDrop3 = 0.10f;

    [Header("Drop Physics")]
    public float dropForce = 5f;
    public float dropRadius = 2f;
    public float upwardForce = 3f;
    public float spawnHeightOffset = 0.5f;
    public LayerMask groundLayerMask = -1;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Singleton para fácil acceso
    public static ItemDropSystem Instance { get; private set; }



    // ================================================= Unity Methods =================================================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void DropFromNormalEnemy(Vector3 position) // Called by normal enemy scripts on death
    {
        DropItems("NormalEnemy", position);
    }

    public void DropFromEliteEnemy(Vector3 position) // Called by elite enemy scripts on death
    {
        DropItems("EliteEnemy", position);
    }

    public void DropFromDestructible(Vector3 position) // Called by destructible object scripts on death
    {
        DropItems("DestructibleObject", position);
    }

    public void DropItems(string sourceType, Vector3 position) // Main method to handle item drops
    {
        DropTable table = GetDropTable(sourceType); // Find the appropriate drop table
        if (table == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[ItemDropSystem] No drop table found for: {sourceType}");
            return;
        }

        int dropCount = GetDropCount(); // Determine how many items to drop
        List<GameObject> droppedItems = new List<GameObject>(); // To keep track of spawned items

        for (int i = 0; i < dropCount; i++) // For each item to drop
        {
            // Choose a random item from the table (can repeat)
            List<DropTable.ItemDrop> validDrops = new List<DropTable.ItemDrop>();
            foreach (var itemDrop in table.possibleDrops)
            {
                if (Random.Range(0f, 100f) <= itemDrop.dropChance)
                    validDrops.Add(itemDrop);
            }

            if (validDrops.Count > 0)
            {
                var chosenDrop = validDrops[Random.Range(0, validDrops.Count)];
                GameObject item = SpawnItem(chosenDrop.itemIndex, position);
                if (item != null)
                    droppedItems.Add(item);
            }
        }

    }


    DropTable GetDropTable(string sourceType) // Find the drop table by source name
    {
        foreach (var table in dropTables)
        {
            if (table.sourceName == sourceType)
                return table;
        }
        return null;
    }

    GameObject SpawnItem(int itemIndex, Vector3 position) // Spawns the item prefab with physics
    {
        if (itemIndex < 0 || itemIndex >= availableItems.Length)
        {
            if (showDebugLogs)
                Debug.LogError($"[ItemDropSystem] Invalid item index: {itemIndex}");
            return null;
        }

        DroppableItem itemData = availableItems[itemIndex]; // Get item data
        if (itemData.prefab == null)
        {
            if (showDebugLogs)
                Debug.LogError($"[ItemDropSystem] No prefab assigned for item: {itemData.itemName}");
            return null;
        }

        int quantity = Random.Range(itemData.minQuantity, itemData.maxQuantity + 1); // Determine quantity to drop
        GameObject lastSpawnedItem = null;

        for (int i = 0; i < quantity; i++) // Spawn each item
        {
            Vector3 randomOffset = Random.insideUnitCircle * dropRadius;
            Vector3 basePosition = position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            // Raycast to find the ground and adjust spawn height to not pass through the floor
            Vector3 spawnPosition = basePosition;
            RaycastHit hit;
            if (Physics.Raycast(basePosition + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayerMask))
            {
                spawnPosition = hit.point + Vector3.up * spawnHeightOffset;
            }
            else
            {
                spawnPosition = basePosition + Vector3.up * 1f;
            }

            Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject item = Instantiate(itemData.prefab, spawnPosition, randomRotation);
            lastSpawnedItem = item;

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = new Vector3(
                    Random.Range(-1f, 1f),
                    upwardForce,
                    Random.Range(-1f, 1f)
                ).normalized;

                rb.AddForce(forceDirection * dropForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * dropForce * 0.5f, ForceMode.Impulse);
            }

        }

        return lastSpawnedItem;
    }

    private int GetDropCount() // Determine how many items to drop based on probabilities
    {
        float roll = Random.value;
        if (roll < probDrop3)
            return 3;
        else if (roll < probDrop3 + probDrop2)
            return 2;
        else
            return 1;
    }



    public void ApplyDifficultyModifier(float multiplier) // Adjust drop chances based on difficulty
    {
        if (showDebugLogs)
            Debug.Log($"[ItemDropSystem] Applied difficulty modifier: x{multiplier}");
    }

    public void ForceDropItem(int itemIndex, Vector3 position, int quantity = 1) // Force drop specific item(s) at position
    {
        for (int i = 0; i < quantity; i++)
        {
            SpawnItem(itemIndex, position);
        }
    }

    // ===== GETTERS =====
    public int GetAvailableItemsCount() => availableItems.Length;
    public string GetItemName(int index) =>
        (index >= 0 && index < availableItems.Length) ? availableItems[index].itemName : "Unknown";
}
