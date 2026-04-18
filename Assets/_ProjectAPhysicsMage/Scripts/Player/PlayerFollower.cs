using UnityEngine;

public class PlayerFollower : MonoBehaviour
{
    private Transform player;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovementController>().transform;
    }
    private void FixedUpdate()
    {
        transform.position = player.position;
    }
}
