using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MagicController : NetworkBehaviour
{
    public readonly SyncVar<MagicElement> element = new SyncVar<MagicElement>();
    public readonly SyncVar<MagicDirection> direction = new SyncVar<MagicDirection>();
    public readonly SyncVar<int> ownerID = new SyncVar<int>();
    private Collider _collider;
    private void Awake() {
        _collider = GetComponent<Collider>();
    }

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
        // Only server handles collision.
        if (!IsServerInitialized)
            return;

        PlayerRagdollController target =
            other.GetComponent<PlayerRagdollController>();

        if (target == null)
            return;

        // Prevent self-hit if needed.
        if (target.OwnerId == ownerID.Value)
            return;

        Vector3 hitDirection =
            GetDirectionBasedOnMagic(
                element.Value,
                direction.Value,
                other.transform.position
            ).normalized;

        float force = 20f;

        // Apply immediately on server (authoritative)
        // target.TriggerFall(hitDirection, force);

        // Replicate to all clients
        TriggerFallObserversRpc(target.ObjectId, hitDirection, force);

        Physics.IgnoreCollision(_collider, other, true);
    }

    [ObserversRpc]
    private void TriggerFallObserversRpc(int targetObjectId, Vector3 dir, float force)
    {
        // Skip server, already applied above
        //if (IsServerInitialized)
        //    return;

        if (NetworkManager.ClientManager.Objects.Spawned
            .TryGetValue(targetObjectId, out NetworkObject nob))
        {
            PlayerRagdollController target =
                nob.GetComponent<PlayerRagdollController>();

            if (target != null)
                target.TriggerFall(dir, force);
        }
    }
}
