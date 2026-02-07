using UnityEngine;

public class ShopStopTrigger : MonoBehaviour
{
    [Header("Allow Shop Entry")]
    public bool unlockShop = false;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que entra tiene el tag del coche
        if (other.CompareTag("Car"))
        {
            MovCar movCar = other.GetComponent<MovCar>();
            if (movCar != null)
            {
                movCar.cur_speed = 0f;
                movCar.enabled = false; // Opcional: desactiva el script de movimiento
                unlockShop = true;
            }
        }
    }
}
