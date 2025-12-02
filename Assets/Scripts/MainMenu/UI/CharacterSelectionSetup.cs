using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

public class CharacterSelectionSetup : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    // ========================================================================================= Prefab Creation ====================================================================================

    public void CreateMenuPlayerPrefab()
    {
        if (!inputActions)
        {
            string[] guids = AssetDatabase.FindAssets("MenuInputs t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            }
            else
            {
                Debug.LogError("MenuInputs not found in project. Please create an InputActionAsset named 'MenuInputs'.");
                return;
            }
        }

        GameObject menuPlayer = new GameObject("MenuPlayer");
        PlayerInput playerInput = menuPlayer.AddComponent<PlayerInput>();
        playerInput.actions = inputActions;
        playerInput.defaultActionMap = "Menu";
        playerInput.notificationBehavior = PlayerNotifications.SendMessages;

        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string prefabPath = $"{prefabFolder}/MenuPlayer.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(menuPlayer, prefabPath);

        DestroyImmediate(menuPlayer);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

    }

    // ========================================================================================= Slot Setup =========================================================================================

    public void ConfigureSlots()
    {
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
                PlayerSlotSimple slot = slotObjects[i].GetComponent<PlayerSlotSimple>();
                if (!slot)
                {
                    slot = slotObjects[i].AddComponent<PlayerSlotSimple>();
                }

                CreateSlotStates(slotObjects[i], i);
            }
        }

    }

    private void CreateSlotStates(GameObject slot, int index)
    {
        Transform idleState = slot.transform.Find("State_Idle");
        Transform joinedState = slot.transform.Find("State_Joined");

        if (!idleState)
        {
            GameObject idle = new GameObject("State_Idle");
            idle.transform.SetParent(slot.transform, false);

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

        if (!joinedState)
        {
            GameObject joined = new GameObject("State_Joined");
            joined.transform.SetParent(slot.transform, false);
            joined.SetActive(false);

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
