using System.Collections;
using UnityEngine;

public class PlayerFollower : MonoBehaviour
{
    private Transform player;

    private void Start()
    {
        StartCoroutine(FindPlayer());
    }
    private IEnumerator FindPlayer()
    {
        while(player == null)
        {
            yield return new WaitForSeconds(1f);
            PlayerMovementController playerMovementController = FindFirstObjectByType<PlayerMovementController>();
            if(playerMovementController != null)
                player = playerMovementController.transform;
        }
    }
    private void FixedUpdate()
    {
        if (player == null) return;
        transform.position = player.position;
    }
}
