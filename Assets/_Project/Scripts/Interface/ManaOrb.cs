using UnityEngine;
using UnityEngine.UI;

public class ManaOrb : MonoBehaviour
{
    public Image frontImage;

    private float currentPercentage;

    public void UpdateOrbPercentage(float percentage)
    {
        currentPercentage = percentage;
        frontImage.fillAmount = currentPercentage;
    }
}
