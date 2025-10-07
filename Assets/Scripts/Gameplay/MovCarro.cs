using UnityEngine;
using System.Collections;

public class MovCarro : MonoBehaviour
{
    public Vector3 direccion = Vector3.forward; // Direcci�n del movimiento (por defecto hacia adelante)
    public float velocidad = 5f; // Velocidad del cubo
    public int diesel = 20;

    void Start()
    {
        // Iniciar la corrutina para perder diesel cada segundo
        StartCoroutine(PerdidaDiesel());
    }

    void Update()
    {
        if (diesel <= 0)
        {
            return; // Si no hay diesel, no mover el cubo
        }

        if (diesel > 0)
        {
            // Mover el objeto en la direcci�n indicada a una velocidad constante
            transform.Translate(direccion.normalized * velocidad * Time.deltaTime, Space.World);
        }
    }

    private IEnumerator PerdidaDiesel()
    {
        while (diesel > 0)
        {
            yield return new WaitForSeconds(1f); // Esperar 1 segundo
            diesel--; // Reducir diesel en 1 unidad
            Debug.Log("Diesel restante: " + diesel); // Para monitorear el diesel en la consola
        }
        
        Debug.Log("¡Se acabó el diesel! El coche se ha detenido.");
    }
}