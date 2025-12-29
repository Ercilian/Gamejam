using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopEntrance : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene("MainShop");
        }
    }
}
