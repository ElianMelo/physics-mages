using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerFeedbackInterfaceController : MonoBehaviour
{
    public TMP_Text manaValue;
    public List<ManaOrb> manaOrbList = new();

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
        for (int i = 0; i < manaOrbList.Count; i++)
        {
            manaOrbList[i].UpdateOrbPercentage(value - i);
        }
    }
}
