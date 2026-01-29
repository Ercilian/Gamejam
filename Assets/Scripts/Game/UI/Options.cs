using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Options : MonoBehaviour
{

    public GameObject OptionsPanel; // panel for options menu
    public GameObject GameOverPanel; // panel for game over menu


    private void Start()
    {
        OptionsPanel.SetActive(false);
        GameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && OptionsPanel.activeSelf)
        {
            Debug.Log("Cerrando opciones");
            CloseSettings();
        }
        else if(Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Abriendo opciones");
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
        Time.timeScale = 0f; // Pause the game
    }   




}
