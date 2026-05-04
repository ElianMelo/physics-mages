using FishNet.Object;
using UnityEngine;

public class ParticleController : NetworkBehaviour
{
    public override void OnStartClient()
    {
        GetComponent<ParticleSystem>().Play();
    }
}
