using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private GameObject healthCanvas;
    [SerializeField] private Image healthImage;
    [SerializeField] private float maxHealth;
    [SerializeField] private float maxMana;

    public PlayerVFXController playerVFXController { get; private set; }

    private readonly SyncVar<float> health = new SyncVar<float>(new SyncTypeSettings(1f));
    private readonly SyncVar<float> mana = new SyncVar<float>(new SyncTypeSettings(0.1f));
    private readonly SyncVar<bool> hasActiveShield = new SyncVar<bool>(new SyncTypeSettings(0.1f));
    private readonly SyncVar<MagicElement> shieldElement = new SyncVar<MagicElement>(new SyncTypeSettings(0.1f));

    public float Mana => mana.Value;

    public static Action<float> OnManaChanged;

    private Coroutine _shieldCoroutine;

    private void Awake()
    {
        playerVFXController = GetComponent<PlayerVFXController>();
    }

    public override void OnStartClient()
    {
        health.Value = maxHealth;
        mana.Value = maxMana;
        hasActiveShield.Value = false;
        health.OnChange += OnHealthChange;
        mana.OnChange += OnManaChange;
        if (!IsOwner) return;
        healthCanvas.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner) return;
    }

    private void OnHealthChange(float prev, float next, bool asServer)
    {
        healthImage.fillAmount = next / maxHealth;
    }

    private void OnManaChange(float prev, float next, bool asServer)
    {
        if (!IsOwner) return;
        OnManaChanged?.Invoke(next);
    }

    public bool HasActiveShield()
    {
        return hasActiveShield.Value;
    }

    public MagicElement GetShieldElement()
    {
        return shieldElement.Value;
    }

    public void SetupShield(float duration, MagicElement element)
    {
        SetupShieldServer(duration, element);
    }

    [ServerRpc(RunLocally = true)]
    private void SetupShieldServer(float duration, MagicElement element)
    {
        hasActiveShield.Value = true;
        shieldElement.Value = element;
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
        _shieldCoroutine = StartCoroutine(ShieldCoroutine());
        IEnumerator ShieldCoroutine()
        {
            yield return new WaitForSeconds(duration);
            hasActiveShield.Value = false;
        }
    }

    public void ReceiveDamage()
    {
        if (!IsOwner) return;
        ReceiveDamageServer(10);
    }

    public void UpdateMana(float amount)
    {
        if (!IsOwner) return;
        UpdateManaServer(amount);
    }

    private void ParticleSpawn()
    {
        NetworkObject nob = NetworkManager.GetPooledInstantiated(PooledObjectsManager.Instance.bloodParticlePrefab,
            transform.position, Quaternion.Euler(-90f, 0f, 0f), IsServerInitialized);
        ServerManager.Spawn(nob);
        StartCoroutine(DelayedDespawn(nob));
        IEnumerator DelayedDespawn(NetworkObject nob) {
            yield return new WaitForSeconds(2f);
            ServerManager.Despawn(nob);
        }
    }

    [ServerRpc]
    private void ReceiveDamageServer(float amount)
    {
        health.Value -= amount;
        ParticleSpawn();
    }

    [ServerRpc]
    private void UpdateManaServer(float amount)
    {
        if (mana.Value + amount > maxMana) return;
        mana.Value += amount;
    }
}
