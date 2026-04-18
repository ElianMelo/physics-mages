using FishNet.Object;
using System;
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

public class PlayerMagicController : NetworkBehaviour
{
    private MagicChoosePhase _currentPhase = MagicChoosePhase.Element;
    private MagicElement _element;
    private MagicDirection _direction;

    public static Action<MagicChoosePhase> OnMagicPhaseChange;

    [SerializeField] private VFXPlayerController playerVFXController;

    void Update()
    {
        if (!IsOwner) return;
        GetMagicInput();
    }

    private void GetMagicInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_currentPhase == MagicChoosePhase.Element)
            {
                _element = MagicElement.Earth; 
                ChangePhase(MagicChoosePhase.Direction);
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
                ChangePhase(MagicChoosePhase.Direction);
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
                ChangePhase(MagicChoosePhase.Direction);
            }
            else
            {
                _direction = MagicDirection.Shield; 
                CastMagic();
            }
        }
    }

    private void ChangePhase(MagicChoosePhase newPhase)
    {
        _currentPhase = newPhase;
        OnMagicPhaseChange?.Invoke(_currentPhase);
    }

    private void CastMagic()
    {
        playerVFXController.CastMagicVFX(_element, _direction);
        ChangePhase(MagicChoosePhase.Element);
    }
}
