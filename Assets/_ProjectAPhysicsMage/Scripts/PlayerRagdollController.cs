using FIMSpace.FProceduralAnimation;
using System.Collections;
using UnityEngine;

public class PlayerRagdollController : MonoBehaviour
{
    private RagdollAnimator2 _ragdoll;
    private Animator _animator;
    private Coroutine _fallCoroutine;
    private float _staggerDuration = 2f;

    private Vector3 _direction;
    private float _force;

    private bool _isStaggered;
    public bool IsStaggered => _isStaggered;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _ragdoll = GetComponent<RagdollAnimator2>();
    }

    public void TriggerFall(Vector3 direction, float force)
    {
        _direction = direction;
        _force = force;
        if (_fallCoroutine != null) StopCoroutine(_fallCoroutine);
        _fallCoroutine = StartCoroutine(FallCoroutine());
    }

    private IEnumerator FallCoroutine()
    {
        _animator.enabled = false;
        _ragdoll.User_AddAllBonesImpact(_direction * _force);
        _ragdoll.RA2Event_SwitchToFall();
        _isStaggered = true;
        yield return new WaitForSeconds(_staggerDuration);
        _animator.enabled = true;
        _isStaggered = false;
        _ragdoll.RA2Event_SwitchToStand();
    }
}
