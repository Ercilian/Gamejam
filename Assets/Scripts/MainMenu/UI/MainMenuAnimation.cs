using UnityEngine;

using UnityEngine.UI;

public class MainMenuAnimation : MonoBehaviour
{
    [Header("Animación de nube UI")]
    public RectTransform cloudRectTransform;
    public Vector2 destino = new Vector2(200f, -100f); //
    public float duracion = 2f;
    public bool autoPlay = true;
    public static bool nubeAnimada = false;

    private Vector2 inicio;
    private float tiempo = 0f;
    private bool animando = false;

    void Start()
    {
        if (cloudRectTransform == null)
            cloudRectTransform = GetComponent<RectTransform>();
        inicio = cloudRectTransform.anchoredPosition;
        if (nubeAnimada)
        {
            // Si ya se animó, forzar estado final
            cloudRectTransform.anchoredPosition = destino;
            cloudRectTransform.localScale = Vector3.one; // Ajusta si usas escala
            animando = false;
        }
        else if (autoPlay)
        {
            animando = true;
        }
    }

    void Update()
    {
        if (!animando) return;
        if (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            cloudRectTransform.anchoredPosition = Vector2.Lerp(inicio, destino, tiempo / duracion);
        }
        else
        {
            cloudRectTransform.anchoredPosition = destino;
            animando = false;
            nubeAnimada = true;
        }
    }
    // Método para forzar la nube a su estado final
    public void ForzarEstadoFinal()
    {
        if (cloudRectTransform == null)
            cloudRectTransform = GetComponent<RectTransform>();
        cloudRectTransform.anchoredPosition = destino;
        cloudRectTransform.localScale = Vector3.one; // Ajusta si usas escala
        animando = false;
        nubeAnimada = true;
    }

    public void PlayAnimation()
    {
        tiempo = 0f;
        inicio = cloudRectTransform.anchoredPosition;
        animando = true;
    }
}
