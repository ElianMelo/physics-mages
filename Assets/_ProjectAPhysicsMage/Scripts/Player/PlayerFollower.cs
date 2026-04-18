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
            player = FindFirstObjectByType<PlayerMovementController>().transform;
        }
    }
    private void FixedUpdate()
    {
        if (player == null) return;
        transform.position = player.position;
    }
}
