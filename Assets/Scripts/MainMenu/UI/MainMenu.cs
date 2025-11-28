using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject SelectCharacterPanel;

    public Button firstSelectedButton;

    [Header("Input System")]
    public InputActionAsset inputActions;

    private InputAction cancelAction;
    public CharacterSelectionManager characterSelectionManager;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        SelectCharacterPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

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
        if (EventSystem.current.currentSelectedGameObject == null && firstSelectedButton != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
    }

    public void Play()
    {
        if (characterSelectionManager != null)
            characterSelectionManager.ResetSelection();

        mainMenuPanel.SetActive(false);
        SelectCharacterPanel.SetActive(true);
        
    }

    public void Settings()
    {
        mainMenuPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }
    
    public void Exit()
    {
        Application.Quit();
    }

    private void OnCancel()
    {
        if (SettingsPanel.activeSelf || SelectCharacterPanel.activeSelf)
        {
            Back();
        }
    }

    public void Back()
    {
        mainMenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        SelectCharacterPanel.SetActive(false);
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
