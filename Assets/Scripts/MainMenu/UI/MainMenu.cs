using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{

    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject selectCharacterPanel;
    public Button firstSelectedButton;
    public Slider masterVolumeSlider;

    [Header("Other")]
    public CharacterSelectionManager characterSelectionManager;
    public InputActionAsset inputActions;
    private InputAction cancelAction;



    // ========================================================================================= Methods ========================================================================================




    void Start()
    {
        // Initialize UI
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        selectCharacterPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize Input Actions
        var uiMap = inputActions.FindActionMap("UI", true);
        cancelAction = uiMap.FindAction("Cancel", true);
        cancelAction.Enable();
        cancelAction.performed += ctx => OnCancel();
    }

    void Update()
    {
        if (Cursor.visible)
            Cursor.visible = false;
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
        if (EventSystem.current.currentSelectedGameObject == null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
    }

    public void Play()
    {
        characterSelectionManager.ResetSelection();
        mainMenuPanel.SetActive(false);
        selectCharacterPanel.SetActive(true);
    }

    public void Settings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(masterVolumeSlider.gameObject);
    }
    
    public void Exit()
    {
        Application.Quit();
    }

    private void OnCancel()
    {
        if (settingsPanel.activeSelf || selectCharacterPanel.activeSelf)
        {
            Back();
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);

        }
    }

    public void Back()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        selectCharacterPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (cancelAction != null)
        {
            cancelAction.Disable();
            cancelAction.Dispose();
        }
    }
}
