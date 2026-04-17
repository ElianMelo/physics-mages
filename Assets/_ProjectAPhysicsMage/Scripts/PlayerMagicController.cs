using UnityEngine;

public enum MagicElement
{
    None,
    Earth,
    Water,
    Wind
}

public enum MagicDirection
{
    None,
    Forward,
    Around,
    Shield
}

public enum MagicChoosePhase
{
    Element,
    Direction
}

public class PlayerMagicController : MonoBehaviour
{
    private MagicChoosePhase _currentPhase = MagicChoosePhase.Element;
    private MagicElement _element;
    private MagicDirection _direction;

    [SerializeField] private VFXPlayerController playerVFXController;

    void Update()
    {
        GetMagicInput();
    }

    private void GetMagicInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_currentPhase == MagicChoosePhase.Element)
            {
                _element = MagicElement.Earth; 
                ChangeToDirectionPhase();
            }
            else
            {
                _direction = MagicDirection.Forward; 
                CastMagic();
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_currentPhase == MagicChoosePhase.Element)
            {
                _element = MagicElement.Water;
                ChangeToDirectionPhase();
            }
            else
            {
                _direction = MagicDirection.Around; 
                CastMagic();
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_currentPhase == MagicChoosePhase.Element)
            {
                _element = MagicElement.Wind;
                ChangeToDirectionPhase();
            }
            else
            {
                _direction = MagicDirection.Shield; 
                CastMagic();
            }
        }
    }

    private void ChangeToDirectionPhase()
    {
        _currentPhase = MagicChoosePhase.Direction;
    }

    private void ChangeToElementPhase()
    {
        _currentPhase = MagicChoosePhase.Element;
    }

    private void CastMagic()
    {
        playerVFXController.CastMagicVFX(_element, _direction);
        ChangeToElementPhase();
    }
}
