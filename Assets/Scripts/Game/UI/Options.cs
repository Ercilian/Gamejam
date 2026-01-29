using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class Options : MonoBehaviour
{

    public GameObject OptionsPanel; // panel for options menu
    public GameObject GameOverPanel; // panel for game over menu
    public Button mainmenubutton; // button to return to main menu from game over


    private void Start()
    {
        OptionsPanel.SetActive(false);
        GameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && OptionsPanel.activeSelf)
        {
            CloseSettings();
        }
        else if(Input.GetKeyDown(KeyCode.Escape))
        {
            OpenSettings();
        }


    }

    public void OpenSettings()
    {
        OptionsPanel.SetActive(true);
        Time.timeScale = 0f; // Pause the game
    }

    public void CloseSettings()
    {
        OptionsPanel.SetActive(false);
        Time.timeScale = 1f; // Resume the game
    }

    public void GameOver()
    {
        GameOverPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(mainmenubutton.gameObject);

        Time.timeScale = 0f; // Pause the game
    }   

    public void ToMainMenu()
    {
        Time.timeScale = 1f; // Resume the game before going to main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }



}
