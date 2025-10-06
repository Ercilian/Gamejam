using UnityEngine;

public class CamaraIsometrica : MonoBehaviour
{
    public Transform objetivo; // El cubo que debe seguir
    public Vector3 offset = new Vector3(-10f, 10f, -10f); // Posici�n de la c�mara relativa al cubo
    public float suavizado = 5f; // Velocidad de interpolaci�n

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Posici�n deseada basada en el cubo + offset
        Vector3 posicionDeseada = objetivo.position + offset;

        // Interpolaci�n suave
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);

        // Mantener la c�mara mirando al cubo
        transform.LookAt(objetivo);
    }
}
