using AYellowpaper.SerializedCollections;
using PixPlays.ElementalVFX;
using System.Collections;
using UnityEngine;

public class VFXController : MonoBehaviour
{
    [System.Serializable]
    public class VFXData
    {
        public float VfxSpawnDelay;
        public BindingPointType Source;
        public float _Duration;
        public float _Radius;
        public bool _FollowPlayer;
        public bool _Reverse;
        public BaseVfx VFX;
    }

    [System.Serializable]
    public struct MagicVFX
    {
        public MagicElement element;
        public MagicDirection direction;

        public MagicVFX(MagicElement element, MagicDirection direction)
        {
            this.element = element;
            this.direction = direction;
        }
    }

    [SerializedDictionary("MagicVFX", "VFXData")]
    public SerializedDictionary<MagicVFX, VFXData> _magicVFXToVFXData;

    [SerializeField] VFXCharacter _VFXCharacter;
    private MagicVFX _currentMagicVFX;
    private VFXData _currentVFXData;

    private int index = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            CastMagicVFX(MagicElement.Wind, MagicDirection.Shield);
        }
    }

    public void CastMagicVFX(MagicElement element, MagicDirection direction)
    {
        _currentMagicVFX = new MagicVFX(element, direction);
        GetDataByCurrentMagic();
        StartCoroutine(SpawnVFX());
    }

    private void GetDataByCurrentMagic()
    {
        if (!_magicVFXToVFXData.ContainsKey(_currentMagicVFX)) return;
        _currentVFXData = _magicVFXToVFXData[_currentMagicVFX];
    }

    IEnumerator SpawnVFX()
    {
        yield return new WaitForSeconds(_currentVFXData.VfxSpawnDelay);
        BaseVfx go = Instantiate(_currentVFXData.VFX);
        if(_currentVFXData._FollowPlayer)
            go.transform.SetParent(transform);
        Transform sourcePoint = _VFXCharacter.BindingPoints.GetBindingPoint(_currentVFXData.Source);
        var vfxData = new VfxData(sourcePoint, _VFXCharacter.GetTarget(), _currentVFXData._Duration, _currentVFXData._Radius);
        vfxData.SetGround(_VFXCharacter.BindingPoints.GetBindingPoint(BindingPointType.Ground));
        go.Play(vfxData);
    }
}