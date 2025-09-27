using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    public GameObject mainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject SelectCharacterPanel;
    void Start()
    {
        mainMenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        SelectCharacterPanel.SetActive(false);
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
