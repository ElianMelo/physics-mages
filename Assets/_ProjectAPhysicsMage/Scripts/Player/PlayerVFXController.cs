using AYellowpaper.SerializedCollections;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public class VFXPlayerController : NetworkBehaviour
{
    private MagicVFX _currentMagicVFX;
    private MagicPrefabData _currentMagicPrefabData;
    [SerializedDictionary("MagicVFX", "PrefabData")]
    public SerializedDictionary<MagicVFX, MagicPrefabData> _magicVFXToPrefabData;

    public void CastMagicVFX(MagicElement element, MagicDirection direction)
    {
        _currentMagicVFX = new MagicVFX(element, direction);
        GetDataByCurrentMagic();
        SpawnVFX(_currentMagicVFX, 
            _currentMagicPrefabData.initialPosition.position, 
            _currentMagicPrefabData.initialPosition.rotation);
    }

    private void GetDataByCurrentMagic()
    {
        if (!_magicVFXToPrefabData.ContainsKey(_currentMagicVFX)) return;
        _currentMagicPrefabData = _magicVFXToPrefabData[_currentMagicVFX];
    }

    [ServerRpc]
    private void SpawnVFX(MagicVFX currentMagicVFX, Vector3 position, Quaternion rotation)
    {
        NetworkObject networkObject;
        _currentMagicPrefabData = _magicVFXToPrefabData[currentMagicVFX];
        networkObject = Instantiate(_currentMagicPrefabData.prefab, position, rotation);
        Spawn(networkObject);
        if (_currentMagicPrefabData.followPlayer)
            networkObject.transform.SetParent(transform);
        if (_currentMagicPrefabData.initialPosition != null)
        {
            networkObject.transform.position = _currentMagicPrefabData.initialPosition.position;
            networkObject.transform.rotation = _currentMagicPrefabData.initialPosition.rotation;
        }
        ParticleSystem particle = networkObject.GetComponent<ParticleSystem>();
        if(particle != null) particle.Play();
        StartCoroutine(DespawnAfterSeconds(networkObject.gameObject, _currentMagicPrefabData.duration));
    }

    private IEnumerator DespawnAfterSeconds(GameObject entitySpawned, float secondsBeforeDespawn)
    {
        yield return new WaitForSeconds(secondsBeforeDespawn);
        Despawn(entitySpawned);
    }
}
