using AYellowpaper.SerializedCollections;
using FishNet.Object;
using PixPlays.ElementalVFX;
using System.Collections;
using UnityEngine;

public class PlayerVFXController : NetworkBehaviour
{
    private MagicVFX _currentMagicVFX;
    private MagicPrefabData _currentMagicPrefabData;
    [SerializedDictionary("MagicVFX", "PrefabData")]
    public SerializedDictionary<MagicVFX, MagicPrefabData> _magicVFXToPrefabData;
    private PlayerController _playerController;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    public void CastMagicVFX(MagicElement element, MagicDirection direction, Vector3 forcedDirection)
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

    [ServerRpc]
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
        magicController.SetupMagicData(currentMagicVFX.element, currentMagicVFX.direction, OwnerId);
        magicController.SetupForcedDirection(forcedDiretion);
        HandleFollowPlayer(magicController, currentMagicVFX);
        Spawn(networkObject);
        ParticleSystem particle = networkObject.GetComponent<ParticleSystem>();
        if(particle != null) particle.Play();
        StartCoroutine(DespawnAfterSeconds(networkObject.gameObject, _currentMagicPrefabData.duration));
    }

    private void HandleFollowPlayer(MagicController magicController, MagicVFX currentMagicVFX)
    {
        if (!_currentMagicPrefabData.followPlayer) return;
        magicController.SetupTarget(transform, new Vector3(0f, currentMagicVFX.element == MagicElement.Earth ? 0.7f : 1f, 0f));
    }

    private void HandleShield(MagicController magicController, MagicVFX currentMagicVFX, float duration)
    {
        if (currentMagicVFX.direction != MagicDirection.Shield) return;
        _playerController.SetupShield(duration, currentMagicVFX.element);
    }

    private IEnumerator DespawnAfterSeconds(GameObject entitySpawned, float secondsBeforeDespawn)
    {
        yield return new WaitForSeconds(secondsBeforeDespawn);
        Despawn(entitySpawned);
    }
}
