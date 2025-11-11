using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{

    public GameObject mainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject SelectCharacterPanel;
    public Button firstSelectedButton;

    
    void Start()
    {
        mainMenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        SelectCharacterPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);

    }

    public void Play()
    {
        mainMenuPanel.SetActive(false);
        SelectCharacterPanel.SetActive(true);
    }

    public void Settings()
    {
        mainMenuPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }

    public void Back()
    {
        mainMenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        SelectCharacterPanel.SetActive(false);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
