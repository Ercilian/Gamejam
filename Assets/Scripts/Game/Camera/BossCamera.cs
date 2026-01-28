using UnityEngine;

public class BossCamera : MonoBehaviour
{
    [Header("Referencias para cámara de boss combat")]
    public Transform cameraBossCombatPosition;
    public Transform cameraBossCombatLookAt;

    void Start()
    {
        if (cameraBossCombatPosition == null || cameraBossCombatLookAt == null)
        {
            Debug.LogWarning("No se han asignado los empties de posición o lookAt en el prefab del mapa");
            return;
        }
        // Buscar la cámara principal de la escena
        CameraMovement camMove = Camera.main != null ? Camera.main.GetComponent<CameraMovement>() : null;
        if (camMove == null)
        {
            Debug.LogWarning("No se encontró CameraMovement en la cámara principal");
            return;
        }
        // Asignar referencias
        camMove.cameraBossCombatPosition = cameraBossCombatPosition;
        camMove.cameraBossCombatLookAt = cameraBossCombatLookAt;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
