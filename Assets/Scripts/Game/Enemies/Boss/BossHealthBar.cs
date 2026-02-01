using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [Header("Referencia al Boss (FrogCombat)")]
    public FrogCombat boss;

    [Header("Imagen de la barra de vida (tipo Filled)")]
    public Image healthBarImage;

    private void Start()
    {
        if (boss == null)
        {
            boss = FindObjectOfType<FrogCombat>();
        }
        if (healthBarImage == null)
        {
            healthBarImage = GetComponentInChildren<Image>();
        }
        UpdateBar();
    }

    private void Update()
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (boss != null && healthBarImage != null)
        {
            float fill = boss.maxHP > 0 ? (float)boss.curHP / boss.maxHP : 0f;
            healthBarImage.fillAmount = Mathf.Clamp01(fill);
        }
    }
}
