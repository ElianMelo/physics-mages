using TMPro;
using UnityEngine;

public class PlayerMagicInterfaceController : MonoBehaviour
{
    public TMP_Text currentPhaseText;
    public TMP_Text qElementDirectionText;
    public TMP_Text eElementDirectionText;
    public TMP_Text fElementDirectionText;

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
                eElementDirectionText.text = "Water";
                fElementDirectionText.text = "Wind";
                break;
            case MagicChoosePhase.Direction:
                qElementDirectionText.text = "Forward";
                eElementDirectionText.text = "Around";
                fElementDirectionText.text = "Shield";
                break;
        }
    }

}
