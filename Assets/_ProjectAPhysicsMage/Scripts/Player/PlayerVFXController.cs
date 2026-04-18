using AYellowpaper.SerializedCollections;
using System.Collections;
using UnityEngine;

public class VFXPlayerController : MonoBehaviour
{
    private MagicVFX _currentMagicVFX;
    private MagicPrefabData _currentMagicPrefabData;
    [SerializedDictionary("MagicVFX", "PrefabData")]
    public SerializedDictionary<MagicVFX, MagicPrefabData> _magicVFXToPrefabData;

    public void CastMagicVFX(MagicElement element, MagicDirection direction)
    {
        _currentMagicVFX = new MagicVFX(element, direction);
        GetDataByCurrentMagic();
        StartCoroutine(SpawnVFX());
    }

    private void GetDataByCurrentMagic()
    {
        if (!_magicVFXToPrefabData.ContainsKey(_currentMagicVFX)) return;
        _currentMagicPrefabData = _magicVFXToPrefabData[_currentMagicVFX];
    }

    IEnumerator SpawnVFX()
    {
        yield return new WaitForSeconds(_currentMagicPrefabData.vfxSpawnDelay);
        var go = Instantiate(_currentMagicPrefabData.prefab);
        if (_currentMagicPrefabData.followPlayer)
            go.transform.SetParent(transform);
        if (_currentMagicPrefabData.initialPosition != null)
        {
            go.transform.position = _currentMagicPrefabData.initialPosition.position;
            go.transform.rotation = _currentMagicPrefabData.initialPosition.rotation;
        }
        go.transform.localScale = new Vector3(_currentMagicPrefabData.scale, _currentMagicPrefabData.scale, _currentMagicPrefabData.scale);
        ParticleSystem particle = go.GetComponent<ParticleSystem>();
        if(particle != null) particle.Play();
        Destroy(go, _currentMagicPrefabData.duration);
    }
}
