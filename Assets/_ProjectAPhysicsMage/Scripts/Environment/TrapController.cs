using UnityEngine;

public class TrapController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController == null) return;
        playerController.ReceiveDamage();
    }

    private void OnCollisionEnter(Collision collision)
    {
        //PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
        //if (playerController == null) return;
        //playerController.ReceiveDamage();
    }
}
