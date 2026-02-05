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
    public GameObject backGroundImage;
    public Button firstSelectedButton;
    public Slider masterVolumeSlider;
    [Header("Animación nube")]
    public MainMenuAnimation nubeAnimacion;
    [Header("Other")]
    public CharacterSelectionManager characterSelectionManager;
    public InputActionAsset inputActions;
    private InputAction cancelAction;



    // ========================================================================================= Methods ========================================================================================




    void Start()
    {
        // Solo mostrar el fondo animado al inicio
        backGroundImage.SetActive(true);
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        selectCharacterPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize Input Actions
        var uiMap = inputActions.FindActionMap("UI", true);
        cancelAction = uiMap.FindAction("Cancel", true);
        cancelAction.Enable();
        cancelAction.performed += ctx => OnCancel();

        // Si la nube ya se animó, forzar estado final y mostrar menú sin esperar
        if (MainMenuAnimation.nubeAnimada)
        {
            if (nubeAnimacion != null)
                nubeAnimacion.ForzarEstadoFinal();
            mainMenuPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
        else
        {
            // Iniciar corrutina para mostrar el menú tras la animación
            StartCoroutine(ShowMenuAfterAnimation());
        }
    }

    private System.Collections.IEnumerator ShowMenuAfterAnimation()
    {
        yield return new WaitForSeconds(2f); // Espera la duración de la animación
        mainMenuPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
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
        backGroundImage.SetActive(false);
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
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        settingsPanel.SetActive(false);
        selectCharacterPanel.SetActive(false);
        backGroundImage.SetActive(true);
        if (nubeAnimacion != null && MainMenuAnimation.nubeAnimada)
            nubeAnimacion.ForzarEstadoFinal();
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
