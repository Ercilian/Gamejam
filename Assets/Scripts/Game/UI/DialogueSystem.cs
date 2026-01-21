
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class DialogueSystem : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerSelectionDataSO playerSelectionData;
    public List<PlayerStatsData> playerStatsList; // Asigna los PlayerStatsData en el orden de characterIndex
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public float dialogueDuration = 2.5f;

    [System.Serializable]
    public class DialogueLine
    {
        public int characterIndex; // Quién habla
        [TextArea]
        public string text;
    }

    [System.Serializable]
    public class DialogueSequence
    {
        public string combinationKey; // Ejemplo: "1_2", "1_3", "2_3", "1_2_3"
        public List<DialogueLine> lines;
    }

    [Header("Diálogos por combinación")]
    public List<DialogueSequence> dialogueSequences;

    private List<DialogueLine> currentDialogue;
    private int currentLineIndex = 0;
    private float timer = 0f;
    private bool dialogueActive = false;

    void Start()
    {
        // El diálogo no se activa automáticamente al iniciar, debe llamarse manualmente
        SetDialogueUIActive(false);
    }

    /// <summary>
    /// Activa el diálogo correspondiente a la combinación actual de jugadores (por defecto)
    /// </summary>
    public void ActivateDialogueForCurrentCombination()
    {
        string key = GetCurrentCombinationKey();
        ActivateDialogueByKey(key);
    }

    /// <summary>
    /// Activa el diálogo correspondiente a una combinación específica (por ejemplo, "1_2", "1_2_3", etc)
    /// </summary>
    public void ActivateDialogueByKey(string combinationKey)
    {
        DialogueSequence sequence = dialogueSequences.FirstOrDefault(ds => ds.combinationKey == combinationKey);
        if (sequence != null && sequence.lines.Count > 0)
        {
            currentDialogue = sequence.lines;
            currentLineIndex = 0;
            dialogueActive = true;
            timer = 0f;
            ShowDialogueLine(currentDialogue[0]);
            SetDialogueUIActive(true);
        }
        else
        {
            dialogueActive = false;
            SetDialogueUIActive(false);
        }
    }

    void Update()
    {
        if (!dialogueActive || currentDialogue == null) return;

        timer += Time.deltaTime;
        if (timer >= dialogueDuration)
        {
            timer = 0f;
            currentLineIndex++;
            if (currentLineIndex < currentDialogue.Count)
            {
                ShowDialogueLine(currentDialogue[currentLineIndex]);
            }
            else
            {
                dialogueActive = false;
                SetDialogueUIActive(false);
                UIManager.Instance.dialoguePanel.SetActive(false);
            }
        }
    }

    private void ShowDialogueLine(DialogueLine line)
    {
        if (playerStatsList != null && line.characterIndex >= 0 && line.characterIndex < playerStatsList.Count)
        {
            var stats = playerStatsList[line.characterIndex];
            if (iconImage != null) iconImage.sprite = stats.PlayerIcon;
            if (nameText != null) nameText.text = stats.PlayerName;
        }
        if (dialogueText != null) dialogueText.text = line.text;
    }

    private void SetDialogueUIActive(bool active)
    {
        if (iconImage != null) iconImage.gameObject.SetActive(active);
        if (nameText != null) nameText.gameObject.SetActive(active);
        if (dialogueText != null) dialogueText.gameObject.SetActive(active);
    }

    // Devuelve una clave única para la combinación de jugadores activos, ej: "1_2_3"
    private string GetCurrentCombinationKey()
    {
        if (playerSelectionData == null || playerSelectionData.selectedPlayers == null)
            return "";
        var indices = playerSelectionData.selectedPlayers.Select(p => p.characterIndex).OrderBy(i => i);
        return string.Join("_", indices);
    }

    public void ActivateDialogueForPasillo(string pasilloId)
    {
        string combinationKey = GetCurrentCombinationKey();
        string fullKey = pasilloId + "_" + combinationKey; // Ejemplo: "Pasillo_1_1_2_3"
        ActivateDialogueByKey(fullKey);
    }
}
