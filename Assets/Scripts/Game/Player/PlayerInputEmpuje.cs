using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputEmpuje : MonoBehaviour
{
    public bool controlActivo = true;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private Transform objetivoASeguir = null;
    private float velocidadSeguir = 0f;
    private PlayerController playerController;
    private bool estoyEmpujandoActualmente = false;
    
    // Input System variables
    private PlayerInput playerInput;
    private InputAction interactAction;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput != null)
        {
            // Usar la acción estándar "Interact" del Input System
            interactAction = playerInput.actions["Interact"];
            
            if (interactAction != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[PlayerInputEmpuje] {gameObject.name} configurado con acción 'Interact' estándar");
            }
            else
            {
                Debug.LogWarning($"[PlayerInputEmpuje] {gameObject.name} - No se encontró acción 'Interact' en el Input Actions");
            }
        }
        else
        {
            Debug.LogError($"[PlayerInputEmpuje] {gameObject.name} no tiene PlayerInput component!");
        }
    }

    public void DesactivarControl()
    {
        if (controlActivo)
        {
            controlActivo = false;
            if (playerController != null) playerController.controlActivo = false;
            
            if (showDebugLogs)
                Debug.Log($"[PlayerInputEmpuje] {gameObject.name} control desactivado");
        }
    }

    public void ActivarControl()
    {
        if (!controlActivo)
        {
            controlActivo = true;
            objetivoASeguir = null;
            estoyEmpujandoActualmente = false;
            if (playerController != null) playerController.controlActivo = true;
            
            if (showDebugLogs)
                Debug.Log($"[PlayerInputEmpuje] {gameObject.name} control activado");
        }
    }

    public void SeguirObjeto(Transform objetivo, float velocidad)
    {
        objetivoASeguir = objetivo;
        velocidadSeguir = velocidad;
    }

    public bool EstaEmpujando()
    {
        if (interactAction == null) return false;
        
        bool pulsando = interactAction.IsPressed();
        
        if (pulsando && showDebugLogs)
            Debug.Log($"[PlayerInputEmpuje] {gameObject.name} empujando via acción 'Interact'");
        
        return pulsando;
    }

    public bool EstoyEmpujandoYo()
    {
        return estoyEmpujandoActualmente;
    }

    void Update()
    {
        if (objetivoASeguir != null)
        {
            bool quieroEmpujar = EstaEmpujando();
            
            if (quieroEmpujar && !estoyEmpujandoActualmente)
            {
                estoyEmpujandoActualmente = true;
                DesactivarControl();
                
                if (showDebugLogs)
                    Debug.Log($"[PlayerInputEmpuje] 🚗 {gameObject.name} comenzó a empujar");
            }
            else if (!quieroEmpujar && estoyEmpujandoActualmente)
            {
                estoyEmpujandoActualmente = false;
                ActivarControl();
                
                if (showDebugLogs)
                    Debug.Log($"[PlayerInputEmpuje] ❌ {gameObject.name} dejó de empujar");
            }
        }
        else
        {
            if (estoyEmpujandoActualmente)
            {
                estoyEmpujandoActualmente = false;
                ActivarControl();
            }
        }

        // Mover hacia el carro solo si YO estoy empujando
        if (!controlActivo && objetivoASeguir != null && estoyEmpujandoActualmente)
        {
            Vector3 destino = objetivoASeguir.position - objetivoASeguir.forward * 1.2f;
            destino.y = transform.position.y;
            destino.z = transform.position.z;

            Vector3 direccion = (destino - transform.position).normalized;
            
            if (Mathf.Abs(destino.x - transform.position.x) > 0.01f)
            {
                transform.position += direccion * velocidadSeguir * Time.deltaTime;
            }
        }
    }
}
