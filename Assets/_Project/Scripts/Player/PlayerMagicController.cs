using FishNet.Component.Animating;
using FishNet.Example.Scened;
using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;

public enum MagicElement
{
    None,
    Earth,
    Water,
    Wind,
    Fire
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
    private bool _isInSafeCheck;

    public static Action<MagicChoosePhase> OnMagicPhaseChange;

    private PlayerVFXController playerVFXController;
    private PlayerController playerController;
    private PlayerRagdollController playerRagdollController;
    private NetworkAnimator animator;
    private Coroutine safeCheckRoutine;

    private static readonly int AnimArea = Animator.StringToHash("Area");
    private static readonly int AnimForward = Animator.StringToHash("Forward");
    private static readonly int AnimShield = Animator.StringToHash("Shield");

    private void Awake()
    {
        playerVFXController = GetComponent<PlayerVFXController>();
        playerController = GetComponent<PlayerController>();
        playerRagdollController = GetComponent<PlayerRagdollController>();
        animator = GetComponent<NetworkAnimator>();    
    }
    public override void OnStartClient()
    {
        if (!IsOwner) return;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (!IsOwner) return;
        GetMagicInput();
    }

    private void GetMagicInput()
    {
        if (_isPerformingMagicAnimation) return;
        if (playerRagdollController.IsStaggered) return;
        if (playerController.Mana < 1f) return;
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
        playerVFXController.ChangeAuraRpc(_element);
        yield return new WaitForSeconds(0f);
        ChangePhase(MagicChoosePhase.Direction);
        _isPerformingMagicAnimation = false;
    }

    private void CastMagic()
    {
        _isPerformingMagicAnimation = true;
        _isInSafeCheck = true;
        playerController.UpdateMana(-1f);
        playerVFXController.ChangeAuraRpc(MagicElement.None);
        if (safeCheckRoutine != null) StopCoroutine(safeCheckRoutine);
        safeCheckRoutine = StartCoroutine(AnimationSafeCheck());
        switch (_direction) 
        {
            case MagicDirection.Forward: animator.SetTrigger(AnimForward); return;
            case MagicDirection.Around: animator.SetTrigger(AnimArea); return;
            case MagicDirection.Shield: animator.SetTrigger(AnimShield); return;
        }
    }

    private IEnumerator AnimationSafeCheck()
    {
        yield return new WaitForSeconds(1.5f);
        _isInSafeCheck = false;
        ChangePhase(MagicChoosePhase.Element);
        _isPerformingMagicAnimation = false;
    }

    public void AnimationCastMagic()
    {
        if (!IsOwner) return;
        if (!_isInSafeCheck) return;
        if (safeCheckRoutine != null) StopCoroutine(safeCheckRoutine);
        playerVFXController.CastMagicVFX(_element, _direction, Camera.main.transform.forward);
        ChangePhase(MagicChoosePhase.Element);
        _isPerformingMagicAnimation = false;
        _isInSafeCheck = false;
    }
}
