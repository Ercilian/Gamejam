using UnityEngine;
using UnityEngine.UI;

public class CarFuelBarUI : MonoBehaviour
{
    [Header("Referencias de velocidad (opcional)")]
    public MovCar movCar;
    [Header("Referencia manual al coche")]
    public Car car;

    [Header("Luces encendidas")]
    public GameObject lightOnLowSpeed;
    public GameObject lightOnMidSpeed;
    public GameObject lightOnHighSpeed;

    [Header("Luces apagadas")]
    public GameObject lightOffLowSpeed;
    public GameObject lightOffMidSpeed;
    public GameObject lightOffHighSpeed;

    [Header("Umbrales de velocidad")]
    public float lowSpeedMax = 5f;
    public float midSpeedMax = 15f;
    // Alta velocidad: > midSpeedMax

    [Header("References")]
    public CarFuelSystem carFuelSystem;
    public Image fuelBarImage;
    public Image hpCarBar;

    void Start()
    {
        if (carFuelSystem == null)
        {
            Debug.LogError("[CarFuelBarUI] Falta referencia a CarFuelSystem");
        }
        if (fuelBarImage == null)
        {
            Debug.LogError("[CarFuelBarUI] Falta referencia a Image de la barra de combustible");
        }
        // La referencia a 'car' debe asignarse manualmente desde el inspector.
        if (car == null)
        {
            Debug.LogError("[CarFuelBarUI] Falta referencia al componente Car. Asigna la referencia manualmente en el inspector.");
        }
    }

    [Header("Animación")]
    public float fillSpeed = 3f; // Velocidad de animación

    private float targetFill = 1f;

    void Update()
    {

        if (carFuelSystem != null && fuelBarImage != null)
        {
            targetFill = carFuelSystem.GetDieselPercentage();
            fuelBarImage.fillAmount = Mathf.Lerp(fuelBarImage.fillAmount, targetFill, Time.deltaTime * fillSpeed);
        }

        // ----- Actualiza la barra de vida del coche -----
        if (car != null && hpCarBar != null)
        {
            float hpPercent = (float)car.CurrentHP / car.MaxHP;
            hpCarBar.fillAmount = Mathf.Lerp(hpCarBar.fillAmount, hpPercent, Time.deltaTime * fillSpeed);
        }

        // ----- Lógica de luces de velocidad -----
        if (movCar != null)
        {
            float speed = movCar.GetCurrentSpeedPublic();

            // Apaga todas
            SetLight(lightOnLowSpeed, false); SetLight(lightOffLowSpeed, true);
            SetLight(lightOnMidSpeed, false); SetLight(lightOffMidSpeed, true);
            SetLight(lightOnHighSpeed, false); SetLight(lightOffHighSpeed, true);

            // Enciende la que toca
            if (speed <= lowSpeedMax)
            {
                SetLight(lightOnLowSpeed, true); SetLight(lightOffLowSpeed, false);
            }
            else if (speed <= midSpeedMax)
            {
                SetLight(lightOnMidSpeed, true); SetLight(lightOffMidSpeed, false);
            }
            else
            {
                SetLight(lightOnHighSpeed, true); SetLight(lightOffHighSpeed, false);
            }
        }
        void SetLight(GameObject go, bool state)
        {
            if (go != null) go.SetActive(state);
        }
    }
}
