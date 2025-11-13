using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject SettingsPanel;
    public GameObject SelectCharacterPanel;
    public Button firstSelectedButton;

    [Header("Input System")]
    public InputActionAsset inputActions; // Asigna tu InputSystem_Actions en el inspector

    private InputAction cancelAction;

    public CharacterSelectionManager characterSelectionManager; // Asigna en el inspector

    void Start()
    {
        mainMenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        SelectCharacterPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);

        // Deshabilita el mouse
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Obtén la acción "Cancel" del Action Map "UI"
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

    private void OnCancel()
    {
        if (SettingsPanel.activeSelf || SelectCharacterPanel.activeSelf)
        {
            Back();
        }
    }

    public void Play()
    {
        if (characterSelectionManager != null)
            characterSelectionManager.ResetSelection(); // Reinicia la selección

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

    void OnDestroy()
    {
        if (cancelAction != null)
        {
            cancelAction.Disable();
            cancelAction.Dispose();
        }
    }
}
