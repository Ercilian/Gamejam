using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de UI")]
    public GameObject dialoguePanel;
    public GameObject difficultyPanel;
    public GameObject charactersPanel;
    public GameObject resourcesPanel;

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void DialogueSetup()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (charactersPanel != null) charactersPanel.SetActive(false);
        if (resourcesPanel != null) resourcesPanel.SetActive(false);
    }

    public void GameUISetup()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(true);
        if (charactersPanel != null) charactersPanel.SetActive(true);
        if (resourcesPanel != null) resourcesPanel.SetActive(true);
    }
}
