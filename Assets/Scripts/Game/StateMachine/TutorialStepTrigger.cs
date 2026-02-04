using UnityEngine;

// Script genérico para triggers de pasos del tutorial
public class TutorialStepTrigger : MonoBehaviour
{
    public int panelIndex = 0; // Índice del panel a mostrar en el TutorialManager
    [Header("Condiciones opcionales")]
    public bool requireNoFuel = false;
    // Puedes añadir más condiciones aquí según lo que necesites

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            bool canShow = true;
            // Condición: sin combustible
            if (requireNoFuel)
            {
                var fuelSystem = other.GetComponent<CarFuelSystem>() ?? other.GetComponentInChildren<CarFuelSystem>();
                if (fuelSystem != null && fuelSystem.HasFuel())
                    canShow = false;
            }
            if (canShow)
                TutorialManager.Instance.ShowPanel(panelIndex);
        }
    }
}
