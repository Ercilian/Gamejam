using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class ShopItemSlot : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text priceText;


    [Header("Data")]
    public UpgradeItemSO upgradeData;
    private bool purchased = false;

    [Header("Audio")]
    public AudioClip purchaseSound;
    private AudioSource audioSource;

    // Llama este método para configurar el slot con los datos del objeto
    public void SetupSlot(UpgradeItemSO data)
    {
        upgradeData = data;
        iconImage.sprite = data.icon;
        priceText.text = data.price.ToString();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (purchased) return;
        // Solo el jugador puede comprar
        var playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;
        var carScrap = FindFirstObjectByType<CarScrapSystem>();
        if (carScrap == null) return;
        if (carScrap.CanAfford(upgradeData.price))
        {
            // Comprar: restar dinero, aplicar efecto y eliminar slot
            carScrap.SpendScrap(upgradeData.price);
            // Sonido de compra
            if (purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
            purchased = true;
            Destroy(gameObject, purchaseSound != null ? purchaseSound.length : 0f);
        }
        else
        {
            // No tiene dinero suficiente: solo lo traspasa
            // (puedes poner feedback visual/sonoro aquí si quieres)
        }
    }
}
