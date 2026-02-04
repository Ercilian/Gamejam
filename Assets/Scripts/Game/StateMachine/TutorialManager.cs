using UnityEngine.InputSystem;
using UnityEngine;

using UnityEngine.UI;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("Input System")]
    public InputActionReference closeTutorialAction; // Asigna la acción "CloseTutorial" desde el Input System
    public float holdTimeToClose = 1.0f; // Segundos a mantener pulsado
    private float holdTimer = 0f;
    private bool isHolding = false;

    [Header("Paneles del tutorial (en orden de aparición)")]
    public List<GameObject> tutorialPanels; // Asigna los paneles del Canvas en el inspector

    private int currentStep = -1;

    public static TutorialManager Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        HideAllPanels();
        if (closeTutorialAction != null)
            closeTutorialAction.action.Enable();
    }
    void OnEnable()
    {
        if (closeTutorialAction != null)
            closeTutorialAction.action.performed += OnCloseTutorialPerformed;
    }

    void OnDisable()
    {
        if (closeTutorialAction != null)
            closeTutorialAction.action.performed -= OnCloseTutorialPerformed;
    }

    private void OnCloseTutorialPerformed(InputAction.CallbackContext ctx)
    {
        // Solo empieza a contar si hay panel activo
        if (IsPanelActive())
            isHolding = true;
    }

    void Update()
    {
        if (IsPanelActive() && closeTutorialAction != null)
        {
            if (closeTutorialAction.action.ReadValue<float>() > 0.5f)
            {
                holdTimer += Time.unscaledDeltaTime;
                if (holdTimer >= holdTimeToClose)
                {
                    HideAllPanels();
                    holdTimer = 0f;
                    isHolding = false;
                }
            }
            else
            {
                holdTimer = 0f;
                isHolding = false;
            }
        }
    }

    private bool IsPanelActive()
    {
        return currentStep >= 0 && currentStep < tutorialPanels.Count && tutorialPanels[currentStep].activeSelf;
    }

    // Llama este método desde triggers para mostrar el panel correspondiente
    public void ShowPanel(int step)
    {
        HideAllPanels();
        if (step >= 0 && step < tutorialPanels.Count)
        {
            tutorialPanels[step].SetActive(true);
            currentStep = step;
            Time.timeScale = 0f; // Pausa el tiempo cuando se muestra un panel
        }
    }

    public void HideAllPanels()
    {
        foreach (var panel in tutorialPanels)
            if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f; // Reanuda el tiempo cuando se ocultan todos los paneles
    }

    // Opcional: avanzar al siguiente panel
    public void NextPanel()
    {
        ShowPanel(currentStep + 1);
    }

    // Opcional: retroceder al panel anterior
    public void PreviousPanel()
    {
        ShowPanel(currentStep - 1);
    }
}
