using FishNet.Object;
using UnityEngine;

[System.Serializable]
public class MagicPrefabData
{
    public Transform initialPosition;
    public bool followPlayer;
    public float duration;
    public NetworkObject prefab;
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