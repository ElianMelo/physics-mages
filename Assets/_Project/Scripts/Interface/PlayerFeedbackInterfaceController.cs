using TMPro;
using UnityEngine;

public class PlayerFeedbackInterfaceController : MonoBehaviour
{
    public TMP_Text manaValue;

    private void Awake()
    {
        PlayerController.OnManaChanged += UpdateMana;
    }

    private void OnDestroy()
    {
        PlayerController.OnManaChanged -= UpdateMana;
    }

    private void UpdateMana(float value)
    {
        manaValue.text = value.ToString();
    }
}
