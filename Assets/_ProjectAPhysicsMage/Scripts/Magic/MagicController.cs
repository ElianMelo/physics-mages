using FishNet.Object;
using FishNet.Object.Synchronizing;
using PixPlays.ElementalVFX;
using UnityEngine;

public class MagicController : NetworkBehaviour
{
    public readonly SyncVar<MagicElement> element = new SyncVar<MagicElement>();
    public readonly SyncVar<MagicDirection> direction = new SyncVar<MagicDirection>();
    public readonly SyncVar<int> ownerID = new SyncVar<int>();
    public readonly SyncVar<int> objectID = new SyncVar<int>();
    private Transform target;
    private Vector3 offset;
    private float smoothTime = 0.05f;
    private Vector3 velocity = Vector3.zero;
    private Collider _collider;
    private Vector3 forcedDirection = Vector3.zero;
    private void Awake() {
        _collider = GetComponent<Collider>();
    }

    private void Update() {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref velocity, smoothTime);
    }

    public override void OnStartClient()
    {
        Shield shield = GetComponent<Shield>();
        if (shield == null) return;
        shield.CallPlayImplementation();
    }

    public void SetupTarget(Transform target, Vector3 offset)
    {
        this.target = target;
        this.offset = offset;
        SetupTargetForClients(offset);
    }

    [ObserversRpc]
    private void SetupTargetForClients(Vector3 offset)
    {
        if (NetworkManager.ClientManager.Objects.Spawned
            .TryGetValue(objectID.Value, out NetworkObject nob))
        {
            PlayerController target =
                nob.GetComponent<PlayerController>();

            this.target = target.transform;
            this.offset = offset;
        }
    }

    public void SetupMagicData(MagicElement element, MagicDirection direction, int ownerID, int objectID)
    {
        this.element.Value = element;
        this.direction.Value = direction;
        this.ownerID.Value = ownerID;
        this.objectID.Value = objectID;
    }

    public void SetupForcedDirection(Vector3 direction)
    {
        forcedDirection = direction;
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

        PlayerController targetController =
            other.GetComponent<PlayerController>();

        if (targetController.HasActiveShield())
        {
            Physics.IgnoreCollision(_collider, other, true);
            HandleShieldBehaviour(targetController);
            return;
        }

        Vector3 hitDirection =
            GetDirectionBasedOnMagic(
                element.Value,
                direction.Value,
                other.transform.position
            ).normalized;

        if(forcedDirection != Vector3.zero)
            hitDirection = forcedDirection.normalized;

        float force = 20f;

        // Replicate to all clients
        TriggerFallObserversRpc(target.ObjectId, hitDirection, force);

        Physics.IgnoreCollision(_collider, other, true);
    }

    private void HandleShieldBehaviour(PlayerController targetController)
    {
        HandleShieldBehaviourClient(targetController.ObjectId);
    }

    [ObserversRpc]
    private void HandleShieldBehaviourClient(int targetObjectId)
    {
        if (NetworkManager.ClientManager.Objects.Spawned
            .TryGetValue(targetObjectId, out NetworkObject nob))
        {
            PlayerController target =
                nob.GetComponent<PlayerController>();

            MagicElement element = target.GetShieldElement();
            if (this.element.Value != element) return;
            target.playerVFXController.CastMagicVFX(this.element.Value, this.direction.Value, transform.position - target.transform.position);
        }
    }

    [ObserversRpc]
    private void TriggerFallObserversRpc(int targetObjectId, Vector3 dir, float force)
    {
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
