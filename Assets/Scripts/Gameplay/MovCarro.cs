using UnityEngine;

public class MovCarro : MonoBehaviour
{
    public Vector3 direccion = Vector3.forward; // Dirección del movimiento (por defecto hacia adelante)
    public float velocidad = 5f; // Velocidad del cubo

    void Update()
    {
        // Mover el objeto en la dirección indicada a una velocidad constante
        transform.Translate(direccion.normalized * velocidad * Time.deltaTime, Space.World);
    }
}
