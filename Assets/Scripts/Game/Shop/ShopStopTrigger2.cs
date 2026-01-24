using UnityEngine;

public class ShopStopTrigger2 : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            MovCar movCar = other.GetComponent<MovCar>();
            if (movCar != null)
            {
                movCar.cur_speed = 0f;
                movCar.enabled = false;
                Debug.Log("Coche parado en la hoguera. ¡Listo para la siguiente fase!");
            }
        }
    }
}
