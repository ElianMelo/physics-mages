using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFeedbackInterfaceController : MonoBehaviour
{
    public List<ManaOrb> manaOrbList = new();
    public Image healthBarImage;
    public Image staminaBarImage;

    private void Awake()
    {
        PlayerController.OnHealthChanged += UpdateHealth;
        PlayerController.OnStaminaChanged += UpdateStamina;
        PlayerController.OnManaChanged += UpdateMana;
    }

    private void OnDestroy()
    {
        PlayerController.OnHealthChanged -= UpdateHealth;
        PlayerController.OnStaminaChanged -= UpdateStamina;
        PlayerController.OnManaChanged -= UpdateMana;
    }

    private void UpdateHealth(float current, float max)
    {
        healthBarImage.fillAmount = current / max;
    }

    private void UpdateStamina(float current, float max)
    {
        staminaBarImage.fillAmount = current / max;
    }

    private void UpdateMana(float current, float max)
    {
        for (int i = 0; i < manaOrbList.Count; i++)
        {
            manaOrbList[i].UpdateOrbPercentage(current - i);
        }
    }
}
