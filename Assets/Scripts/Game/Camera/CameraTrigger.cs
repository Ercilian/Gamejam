using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    public Vector3 newOffset = new Vector3(0f, 5f, -5f);
    public float delaySeconds = 2f;
    [SerializeField]private CameraMovement cameraMovement;

    private void Awake()
    {
        // Busca el componente CameraMovement en la escena (puedes ajustar si tienes varias cámaras)
        cameraMovement = FindObjectOfType<CameraMovement>();
        if (cameraMovement == null)
        {
            Debug.LogError("No se encontró CameraMovement en la escena.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car") && cameraMovement != null)
        {
            Debug.Log("Car entered trigger, changing camera offset.");
            cameraMovement.ChangeOffsetWithDelay(newOffset, delaySeconds);
        }
    }
}
