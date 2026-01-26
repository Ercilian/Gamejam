using UnityEngine;
using UnityEngine.UI;

public class CarFuelBarUI : MonoBehaviour
{
    [Header("Referencias de velocidad (opcional)")]
    public MovCar movCar; // Referencia al script que tiene la velocidad

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
    public Image fuelBarImage; // Debe ser tipo Filled (Fill Method Horizontal)

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
        // Ya no se llama a UpdateFuelBar();
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
