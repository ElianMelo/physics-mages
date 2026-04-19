using FishNet.Object;
using UnityEngine;

public class MagicController : NetworkBehaviour
{
    private MagicElement element;
    private MagicDirection direction;
    private void Awake() { }

    private void Update() { }

    public void SetupMagicData(MagicElement element, MagicDirection direction)
    {
        this.element = element;
        this.direction = direction;
    }

    private Vector3 GetDirectionBasedOnMagic(MagicElement element, MagicDirection direction, Vector3 otherPosition)
    {
        Vector3 toOtherDirection = otherPosition - transform.position;
        switch (direction)
        {
            case MagicDirection.Forward:
                {
                    switch (element)
                    {
                        case MagicElement.Earth: return Vector3.up + toOtherDirection;
                        case MagicElement.Water: return Vector3.down + toOtherDirection;
                        case MagicElement.Wind: return toOtherDirection; // Right or left
                    }
                }
                break;
            case MagicDirection.Around:
                {
                    switch (element)
                    {
                        case MagicElement.Earth: return Vector3.up + toOtherDirection;
                        case MagicElement.Water: return Vector3.down + toOtherDirection;
                        case MagicElement.Wind: return toOtherDirection; // Right or left
                    }
                }
                break;
            case MagicDirection.Shield:
                return Vector3.zero;
        }
        return Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerRagdollController playerRagdollController = other.GetComponent<PlayerRagdollController>();
        if (playerRagdollController == null) return;
        // GetDirectionBasedOnMagic(element, direction, other.transform.position)
        playerRagdollController.TriggerFall(Vector3.up, 20f);
        Physics.IgnoreCollision(GetComponent<Collider>(), other, true);
    }
}
