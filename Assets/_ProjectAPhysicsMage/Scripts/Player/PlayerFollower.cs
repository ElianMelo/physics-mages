using UnityEngine;

public class PlayerFollower : MonoBehaviour
{
    private Transform player;

    public void SetupPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        transform.position = player.position;
    }
}
