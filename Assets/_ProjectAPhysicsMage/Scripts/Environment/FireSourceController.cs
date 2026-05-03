using AYellowpaper.SerializedCollections;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public class FireSourceController : NetworkBehaviour
{
    private MagicVFX _currentMagicVFX;
    private MagicPrefabData _currentMagicPrefabData;
    private bool isFireActive = true;
    private float disabledDuration = 3f;
    private int _currentOwnerID;
    private int _currentObjectID;
    [SerializedDictionary("MagicVFX", "PrefabData")]
    public SerializedDictionary<MagicVFX, MagicPrefabData> _magicVFXToPrefabData;
    public Light lightSource;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServerInitialized) return;
        MagicController magicController = other.GetComponent<MagicController>();
        if (magicController == null) return;
        if (!isFireActive) return;
        _currentOwnerID = magicController.ownerID.Value;
        _currentObjectID = magicController.objectID.Value;
        if (magicController.element.Value == MagicElement.Wind)
        {
            CastMagicVFX(MagicElement.Fire, magicController.direction.Value, 
                (transform.position - other.transform.position).normalized);
        }
        if (magicController.element.Value == MagicElement.Water)
        {
            StartCoroutine(SwitchActiveFireSource());
        }
    }

    private IEnumerator SwitchActiveFireSource()
    {
        isFireActive = false;
        lightSource.intensity = 0f;
        yield return new WaitForSeconds(disabledDuration);
        lightSource.intensity = 1f;
        isFireActive = true;
    }

    private void CastMagicVFX(MagicElement element, MagicDirection direction, Vector3 forcedDirection)
    {
        _currentMagicVFX = new MagicVFX(element, direction);
        GetDataByCurrentMagic();
        SpawnVFX(_currentMagicVFX,
            _currentMagicPrefabData.initialPosition.position,
            _currentMagicPrefabData.initialPosition.rotation,
            forcedDirection);
    }

    private void GetDataByCurrentMagic()
    {
        if (!_magicVFXToPrefabData.ContainsKey(_currentMagicVFX)) return;
        _currentMagicPrefabData = _magicVFXToPrefabData[_currentMagicVFX];
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnVFX(MagicVFX currentMagicVFX, Vector3 position, Quaternion rotation, Vector3 forcedDiretion)
    {
        NetworkObject networkObject;
        _currentMagicPrefabData = _magicVFXToPrefabData[currentMagicVFX];
        Vector3 targetPosition = position;
        Quaternion targetRotation = rotation;
        if (_currentMagicPrefabData.initialPosition != null)
        {
            targetPosition = _currentMagicPrefabData.initialPosition.position;
            targetRotation = _currentMagicPrefabData.initialPosition.rotation;
            if (forcedDiretion != Vector3.zero)
                targetRotation = Quaternion.LookRotation(forcedDiretion);
        }
        networkObject = Instantiate(_currentMagicPrefabData.prefab, targetPosition, targetRotation);
        MagicController magicController = networkObject.GetComponent<MagicController>();
        HandleShield(magicController, currentMagicVFX, _currentMagicPrefabData.duration);
        magicController.SetupMagicData(currentMagicVFX.element, currentMagicVFX.direction, _currentOwnerID, _currentObjectID);
        magicController.SetupForcedDirection(forcedDiretion);
        Spawn(networkObject);
        HandleFollowPlayer(magicController, currentMagicVFX);
        ParticleSystem particle = networkObject.GetComponent<ParticleSystem>();
        if (particle != null) particle.Play();
        StartCoroutine(DespawnAfterSeconds(networkObject.gameObject, _currentMagicPrefabData.duration));
    }

    private void HandleFollowPlayer(MagicController magicController, MagicVFX currentMagicVFX)
    {
        if (!_currentMagicPrefabData.followPlayer) return;
        PlayerController target = GetPlayerByObjectID();
        Transform targetTransform = target == null ? this.transform : target.transform;

        magicController.SetupTarget(targetTransform, new Vector3(0f, currentMagicVFX.element == MagicElement.Earth ? 0.7f : 1f, 0f));
    }

    private PlayerController GetPlayerByObjectID()
    {
        if (NetworkManager.ClientManager.Objects.Spawned
            .TryGetValue(_currentObjectID, out NetworkObject nob))
        {
            PlayerController target =
                nob.GetComponent<PlayerController>();

            return target;
        }
        return null;
    }

    private void HandleShield(MagicController magicController, MagicVFX currentMagicVFX, float duration)
    {
        if (currentMagicVFX.direction != MagicDirection.Shield) return;
        PlayerController target = GetPlayerByObjectID();
        if (target == null) return;
        target.SetupShield(duration, currentMagicVFX.element);
    }

    private IEnumerator DespawnAfterSeconds(GameObject entitySpawned, float secondsBeforeDespawn)
    {
        yield return new WaitForSeconds(secondsBeforeDespawn);
        Despawn(entitySpawned);
    }
}
