using UnityEngine;

public class PlayerInputEmpuje : MonoBehaviour
{
    public bool controlActivo = true;
    private Transform objetivoASeguir = null;
    private float velocidadSeguir = 0f;
    private PlayerController playerController;

    // Referencia opcional al script real de movimiento
    // [SerializeField] private PlayerMovement playerMovement;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    // Llama esto desde MovCarro para desactivar el control
    public void DesactivarControl()
    {
        if (controlActivo)
        {
            controlActivo = false;
            if (playerController != null) playerController.controlActivo = false;
            // Desactiva aquí tu script de movimiento real
            // if (playerMovement != null) playerMovement.enabled = false;
        }
    }

    public void ActivarControl()
    {
        if (!controlActivo)
        {
            controlActivo = true;
            objetivoASeguir = null;
            if (playerController != null) playerController.controlActivo = true;
            // Activa aquí tu script de movimiento real
            // if (playerMovement != null) playerMovement.enabled = true;
        }
    }

    public void SeguirObjeto(Transform objetivo, float velocidad)
    {
        objetivoASeguir = objetivo;
        velocidadSeguir = velocidad;
    }

    public bool EstaEmpujando(KeyCode teclaEmpujar)
    {
        bool pulsando = Input.GetKey(teclaEmpujar);
        if (pulsando)
            Debug.Log("[PlayerInputEmpuje] Pulsando tecla de empujar");
        return pulsando;
    }

    void Update()
    {
        // Si está siguiendo el coche y pulsando la tecla de empujar, desactiva el control
        if (objetivoASeguir != null && Input.GetKey(KeyCode.E))
        {
            DesactivarControl();
        }
        else
        {
            ActivarControl();
        }

        if (!controlActivo && objetivoASeguir != null)
        {
            // Calcula el destino detrás del coche
            Vector3 destino = objetivoASeguir.position - objetivoASeguir.forward * 1.2f;
            destino.y = transform.position.y; // Mantener la altura del jugador
            destino.z = transform.position.z; // Mantener la posición Z del jugador (no moverse en Z)

            Vector3 direccion = (destino - transform.position).normalized;
            // Solo mover si hay distancia en X (evita división por cero)
            if (Mathf.Abs(destino.x - transform.position.x) > 0.01f)
            {
                transform.position += direccion * velocidadSeguir * Time.deltaTime;
            }
        }
    }
}
