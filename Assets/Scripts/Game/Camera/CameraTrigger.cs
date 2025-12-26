using UnityEngine;

public class CameraTrigger : MonoBehaviour
// Script to change camera offset when the car enters the trigger zone (cinematic camera).
{
    public Vector3 newOffset = new Vector3(0f, 5f, -5f);
    public float delaySeconds = 2f;
    [SerializeField]private CameraMovement cameraMovement;

    private void Awake() // Search for CameraMovement in the scene
    {
        cameraMovement = FindFirstObjectByType<CameraMovement>();
    }

    private void OnTriggerEnter(Collider other) // Detect when the car enters the trigger zone and change camera offset
    {
        if (other.CompareTag("Car") && cameraMovement != null)
        {
            cameraMovement.ChangeOffsetWithDelay(newOffset, delaySeconds);
        }
    }
}
