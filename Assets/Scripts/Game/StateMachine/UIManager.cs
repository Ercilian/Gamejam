using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de UI")]
    public GameObject dialoguePanel;
    public GameObject difficultyPanel;
    public GameObject charactersPanel;
    public GameObject resourcesPanel;
    public GameObject bossFightPanel;

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
        dialoguePanel.SetActive(true);
        difficultyPanel.SetActive(false);
        charactersPanel.SetActive(false);
        resourcesPanel.SetActive(false);
    }

    public void GameUISetup()
    {
        dialoguePanel.SetActive(false);
        difficultyPanel.SetActive(true);
        charactersPanel.SetActive(true);
        resourcesPanel.SetActive(true);
    }

    public void BossDialogueSetup()
    {
        bossFightPanel.SetActive(false);
        dialoguePanel.SetActive(false);
        difficultyPanel.SetActive(false);
        charactersPanel.SetActive(false);
        resourcesPanel.SetActive(false);
    }
    
    public void BossFightSetup()
    {
        bossFightPanel.SetActive(true);
        dialoguePanel.SetActive(false);
        difficultyPanel.SetActive(false);
        charactersPanel.SetActive(true);
        resourcesPanel.SetActive(false);
    }
}
