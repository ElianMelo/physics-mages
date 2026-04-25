using FishNet.Object;
using System;
using System.Collections;
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
    private bool _isPerformingMagicAnimation;

    public static Action<MagicChoosePhase> OnMagicPhaseChange;

    private PlayerVFXController playerVFXController;
    private Animator animator;

    private static readonly int AnimArea = Animator.StringToHash("Area");
    private static readonly int AnimForward = Animator.StringToHash("Forward");
    private static readonly int AnimShield = Animator.StringToHash("Shield");

    private void Awake()
    {
        playerVFXController = GetComponent<PlayerVFXController>();
        animator = GetComponentInChildren<Animator>();    
    }

    void Update()
    {
        if (!IsOwner) return;
        GetMagicInput();
    }

    private void GetMagicInput()
    {
        if (_isPerformingMagicAnimation) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (_currentPhase == MagicChoosePhase.Element)
            {
                _element = MagicElement.Earth;
                CastElement();
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
                CastElement();
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
                CastElement();
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

    private void CastElement()
    {
        StartCoroutine(CastElementCoroutine());
    }

    private IEnumerator CastElementCoroutine()
    {
        _isPerformingMagicAnimation = true;
        yield return new WaitForSeconds(0f);
        ChangePhase(MagicChoosePhase.Direction);
        _isPerformingMagicAnimation = false;
    }

    private void CastMagic()
    {
        _isPerformingMagicAnimation = true;
        switch (_direction) 
        {
            case MagicDirection.Forward: animator.SetTrigger(AnimForward); return;
            case MagicDirection.Around: animator.SetTrigger(AnimArea); return;
            case MagicDirection.Shield: animator.SetTrigger(AnimShield); return;
        }
    }

    public void AnimationCastMagic()
    {
        playerVFXController.CastMagicVFX(_element, _direction);
        ChangePhase(MagicChoosePhase.Element);
        _isPerformingMagicAnimation = false;
    }
}
