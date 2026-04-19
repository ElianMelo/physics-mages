using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class MagicController : NetworkBehaviour
{
    public readonly SyncVar<MagicElement> element = new SyncVar<MagicElement>();
    public readonly SyncVar<MagicDirection> direction = new SyncVar<MagicDirection>();
    public readonly SyncVar<int> ownerID = new SyncVar<int>();
    private void Awake() { }

    private void Update() { }

    public void SetupMagicData(MagicElement element, MagicDirection direction, int ownerID)
    {
        this.element.Value = element;
        this.direction.Value = direction;
        this.ownerID.Value = ownerID;
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
        if (playerRagdollController.OwnerId == ownerID.Value) return;
        playerRagdollController.TriggerFall(GetDirectionBasedOnMagic(element.Value, direction.Value, other.transform.position).normalized, 20f);
        Physics.IgnoreCollision(GetComponent<Collider>(), other, true);
    }
}
