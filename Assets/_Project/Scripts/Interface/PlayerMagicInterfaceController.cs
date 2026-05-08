using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMagicInterfaceController : MonoBehaviour
{
    public TMP_Text currentPhaseText;
    public TMP_Text qElementDirectionText;
    public TMP_Text eElementDirectionText;
    public TMP_Text fElementDirectionText;

    public Image indicatorImageOne;
    public Image indicatorImageTwo;
    public Image indicatorImageThree;

    public Sprite elementEarth;
    public Sprite elementWater;
    public Sprite elementWind;

    public Sprite directionForward;
    public Sprite directionAround;
    public Sprite directionShield;

    private void Awake()
    {
        PlayerMagicController.OnMagicPhaseChange += UpdateFieldsText;
    }

    private void OnDestroy()
    {
        PlayerMagicController.OnMagicPhaseChange -= UpdateFieldsText;
    }

    private void UpdateFieldsText(MagicChoosePhase currentPhase)
    {
        currentPhaseText.text = "Select: " + currentPhase.ToString();
        switch (currentPhase)   
        {
            case MagicChoosePhase.Element:
                qElementDirectionText.text = "Earth";
                indicatorImageOne.sprite = elementEarth;
                eElementDirectionText.text = "Water";
                indicatorImageTwo.sprite = elementWater;
                fElementDirectionText.text = "Wind";
                indicatorImageThree.sprite = elementWind;
                break;
            case MagicChoosePhase.Direction:
                qElementDirectionText.text = "Forward";
                indicatorImageOne.sprite = directionForward;
                eElementDirectionText.text = "Around";
                indicatorImageTwo.sprite = directionAround;
                fElementDirectionText.text = "Shield";
                indicatorImageThree.sprite = directionShield;
                break;
        }
    }

}
