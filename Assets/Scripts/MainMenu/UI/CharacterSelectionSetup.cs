using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;

public class CharacterSelectionSetup : MonoBehaviour
{
    [Header("Configuración Automática")]
    [SerializeField] private InputActionAsset inputActions;
    
    [ContextMenu("Crear MenuPlayer Prefab")]
    public void CreateMenuPlayerPrefab()
    {
        // Buscar el InputActionAsset si no está asignado
        if (!inputActions)
        {
            string[] guids = AssetDatabase.FindAssets("MenuInputs t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                Debug.Log($"InputActionAsset encontrado: {path}");
            }
            else
            {
                Debug.LogError("No se encontró MenuInputs.inputactions. Asegúrate de que existe.");
                return;
            }
        }

        // Crear GameObject MenuPlayer
        GameObject menuPlayer = new GameObject("MenuPlayer");
        
        // Añadir y configurar PlayerInput
        PlayerInput playerInput = menuPlayer.AddComponent<PlayerInput>();
        playerInput.actions = inputActions;
        playerInput.defaultActionMap = "Menu";
        playerInput.notificationBehavior = PlayerNotifications.SendMessages;

        // Crear prefab
        string prefabPath = "Assets/Prefabs/MenuPlayer.prefab";
        
        // Crear carpeta Prefabs si no existe
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Guardar prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(menuPlayer, prefabPath);
        
        // Limpiar GameObject temporal
        DestroyImmediate(menuPlayer);
        
        // Seleccionar el prefab creado
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"MenuPlayer prefab creado en: {prefabPath}");
    }

    [ContextMenu("Configurar Slots Automáticamente")]
    public void ConfigureSlots()
    {
        // Buscar todos los SlotTemplate en la escena
        GameObject[] slotObjects = {
            GameObject.Find("SlotTemplate"),
            GameObject.Find("SlotTemplate (1)"),
            GameObject.Find("SlotTemplate (2)"),
            GameObject.Find("SlotTemplate (3)")
        };

        for (int i = 0; i < slotObjects.Length; i++)
        {
            if (slotObjects[i] != null)
            {
                // Añadir PlayerSlotSimple si no existe
                PlayerSlotSimple slot = slotObjects[i].GetComponent<PlayerSlotSimple>();
                if (!slot)
                {
                    slot = slotObjects[i].AddComponent<PlayerSlotSimple>();
                    Debug.Log($"Añadido PlayerSlotSimple a {slotObjects[i].name}");
                }

                // Configurar estados si no existen
                CreateSlotStates(slotObjects[i], i);
            }
        }

        Debug.Log("Configuración de slots completada");
    }

    private void CreateSlotStates(GameObject slot, int index)
    {
        Transform idleState = slot.transform.Find("State_Idle");
        Transform joinedState = slot.transform.Find("State_Joined");

        // Crear State_Idle si no existe
        if (!idleState)
        {
            GameObject idle = new GameObject("State_Idle");
            idle.transform.SetParent(slot.transform, false);
            
            // Añadir texto
            GameObject idleText = new GameObject("Text");
            idleText.transform.SetParent(idle.transform, false);
            var textComp = idleText.AddComponent<TMPro.TextMeshProUGUI>();
            textComp.text = "Press Any Button";
            textComp.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform rectTransform = idleText.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        // Crear State_Joined si no existe
        if (!joinedState)
        {
            GameObject joined = new GameObject("State_Joined");
            joined.transform.SetParent(slot.transform, false);
            joined.SetActive(false);
            
            // Añadir texto
            GameObject joinedText = new GameObject("PlayerText");
            joinedText.transform.SetParent(joined.transform, false);
            var textComp = joinedText.AddComponent<TMPro.TextMeshProUGUI>();
            textComp.text = $"PLAYER {index + 1}";
            textComp.alignment = TMPro.TextAlignmentOptions.Center;
            textComp.color = Color.green;
            
            RectTransform rectTransform = joinedText.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
#endif