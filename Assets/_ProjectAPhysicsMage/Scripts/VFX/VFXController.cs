using PixPlays.ElementalVFX;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXController : MonoBehaviour
{
    [System.Serializable]
    public class VFXData
    {
        public string Name;
        public AnimationClip clip;
        public float VfxSpawnDelay;
        public BindingPointType Source;
        public float _Duration;
        public float _Radius;
        public bool _FollowPlayer;
        public BaseVfx VFX;
    }

    [SerializeField] List<VFXData> _Data;
    [SerializeField] VFXCharacter _VFXCharacter;
    [SerializeField] string _CurrentData;

    private int index = 0;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
        StartCoroutine(Coroutine_Spawn());    
        }
    }

    IEnumerator Coroutine_Spawn()
    {
        _VFXCharacter.PlayAnimation("New Animation", _Data[index].clip);
        yield return new WaitForSeconds(_Data[index].VfxSpawnDelay);
        BaseVfx go = Instantiate(_Data[index].VFX);
        if(_Data[index]._FollowPlayer)
            go.transform.SetParent(transform);
        Transform sourcePoint = _VFXCharacter.BindingPoints.GetBindingPoint(_Data[index].Source);
        var vfxData = new VfxData(sourcePoint, _VFXCharacter.GetTarget(), _Data[index]._Duration, _Data[index]._Radius);
        vfxData.SetGround(_VFXCharacter.BindingPoints.GetBindingPoint(BindingPointType.Ground));
        go.Play(vfxData);
    }
}