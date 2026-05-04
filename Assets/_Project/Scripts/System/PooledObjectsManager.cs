using FishNet;
using FishNet.Object;
using UnityEngine;

public class PooledObjectsManager : NetworkBehaviour
{
    public static PooledObjectsManager Instance;

    public NetworkObject bloodParticlePrefab;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        /// <summary>
        /// Instantiates a number of objects and adds them to the pool.
        /// </summary>
        /// <param name="prefab">Prefab to cache.</param>
        /// <param name="count">Quantity to spawn.</param>
        /// <param name="asServer">True if storing prefabs for the server collection.</param>
        InstanceFinder.NetworkManager.CacheObjects(bloodParticlePrefab, 5, IsServerInitialized);
    }
}
