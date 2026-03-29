using FIMSpace.FProceduralAnimation;
using System.Collections;
using UnityEngine;

public class PlayerRagdollController : MonoBehaviour
{
    private RagdollAnimator2 ragdoll;
    private Animator animator;
    private Coroutine fallCoroutine;
    private float staggerDuration = 2f;

    private bool _isStaggered;
    public bool IsStaggered => _isStaggered;

    private void Start()
    {
        animator = GetComponent<Animator>();
        ragdoll = GetComponent<RagdollAnimator2>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.B))
        {
            TriggerFall();
        }
    }

    private void TriggerFall()
    {
        if (fallCoroutine != null) StopCoroutine(fallCoroutine);
        fallCoroutine = StartCoroutine(FallCoroutine());
    }

    private IEnumerator FallCoroutine()
    {
        animator.enabled = false;
        ragdoll.RA2Event_SwitchToFall();
        _isStaggered = true;
        yield return new WaitForSeconds(staggerDuration);
        animator.enabled = true;
        _isStaggered = false;
        ragdoll.RA2Event_SwitchToStand();
    }
}
