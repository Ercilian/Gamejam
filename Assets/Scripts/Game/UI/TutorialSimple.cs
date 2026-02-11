using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class TutorialSimple : MonoBehaviour
{
    [Header("Tutorial Images")]
    public Sprite[] tutorialSprites; // Asigna aquí las imágenes del tutorial
    public Image tutorialImage; // El componente Image donde se muestra la imagen
    public Button nextButton;
    public Button prevButton;
    public Button backButton; // Asigna aquí el botón Back desde el inspector

    private int currentIndex = 0;

    void Start()
    {
        ShowPage(0);
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PrevPage);
    }

    public void ShowPage(int index)
    {
        if (tutorialSprites.Length == 0) return;
        currentIndex = Mathf.Clamp(index, 0, tutorialSprites.Length - 1);
        tutorialImage.sprite = tutorialSprites[currentIndex];
        prevButton.interactable = currentIndex > 0;
        nextButton.interactable = currentIndex < tutorialSprites.Length - 1;

        // Si estamos en la última página y el botón Next se desactiva, selecciona Back
        if (!nextButton.interactable && backButton != null)
        {
            EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }
    }

    public void NextPage()
    {
        if (currentIndex < tutorialSprites.Length - 1)
            ShowPage(currentIndex + 1);
    }

    public void PrevPage()
    {
        if (currentIndex > 0)
            ShowPage(currentIndex - 1);
    }
}
