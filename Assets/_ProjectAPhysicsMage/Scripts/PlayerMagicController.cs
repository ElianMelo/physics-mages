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

    [SerializeField] private GameObject _earthForward;
    [SerializeField] private GameObject _earthAround;
    [SerializeField] private GameObject _earthShield;
    [SerializeField] private GameObject _waterForward;
    [SerializeField] private GameObject _waterAround;
    [SerializeField] private GameObject _waterShield;
    [SerializeField] private GameObject _windForward;
    [SerializeField] private GameObject _windAround;
    [SerializeField] private GameObject _windShield;

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
        GameObject prefab = CalculatedPrefabBasedOnData();
        ChangeToElementPhase();
    }

    private GameObject CalculatedPrefabBasedOnData()
    {
        if (_element == MagicElement.Earth && _direction == MagicDirection.Forward)
            return _earthForward;
        if (_element == MagicElement.Earth && _direction == MagicDirection.Around)
            return _earthAround;
        if (_element == MagicElement.Earth && _direction == MagicDirection.Shield)
            return _earthShield;
        if (_element == MagicElement.Water && _direction == MagicDirection.Forward)
            return _waterForward;
        if (_element == MagicElement.Water && _direction == MagicDirection.Around)
            return _waterAround;
        if (_element == MagicElement.Water && _direction == MagicDirection.Shield)
            return _waterShield;
        if (_element == MagicElement.Wind && _direction == MagicDirection.Forward)
            return _windForward;
        if (_element == MagicElement.Wind && _direction == MagicDirection.Around)
            return _windAround;
        if (_element == MagicElement.Wind && _direction == MagicDirection.Shield)
            return _windShield;
        Debug.LogError("Unexpected error!");
        return _earthForward;
    }
}
