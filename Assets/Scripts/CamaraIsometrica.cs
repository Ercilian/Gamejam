using UnityEngine;

public class CamaraIsometrica : MonoBehaviour
{
    public Transform objetivo; // El cubo que debe seguir
    public Vector3 offset = new Vector3(-10f, 10f, -10f); // Posición de la cámara relativa al cubo
    public float suavizado = 5f; // Velocidad de interpolación

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Posición deseada basada en el cubo + offset
        Vector3 posicionDeseada = objetivo.position + offset;

        // Interpolación suave
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);

        // Mantener la cámara mirando al cubo
        transform.LookAt(objetivo);
    }
}
