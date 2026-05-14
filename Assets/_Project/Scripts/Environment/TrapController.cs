using UnityEngine;

public class TrapController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController == null) return;
        PlayerRagdollController playerRagdollController = playerController.GetComponent<PlayerRagdollController>();
        playerController.ReceiveDamage();
        playerRagdollController.TriggerFall((other.transform.position - transform.position).normalized, 50f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
        //if (playerController == null) return;
        //playerController.ReceiveDamage();
    }
}
