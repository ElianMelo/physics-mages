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
    [SerializeField] private float maxStamina;

    public PlayerVFXController playerVFXController { get; private set; }

    private readonly SyncVar<float> health = new SyncVar<float>(new SyncTypeSettings(1f));
    private readonly SyncVar<float> mana = new SyncVar<float>(new SyncTypeSettings(0.1f));
    private readonly SyncVar<float> stamina = new SyncVar<float>(new SyncTypeSettings(0.1f));
    private readonly SyncVar<bool> hasActiveShield = new SyncVar<bool>(new SyncTypeSettings(0.1f));
    private readonly SyncVar<MagicElement> shieldElement = new SyncVar<MagicElement>(new SyncTypeSettings(0.1f));

    public float Mana => mana.Value;
    public float Stamina => stamina.Value;
    public float Health => health.Value;
    public float MaxMana => maxMana;
    public float MaxStamina => maxStamina;
    public float MaxHealth => maxHealth;

    public static Action<float, float> OnManaChanged;
    public static Action<float, float> OnHealthChanged;
    public static Action<float, float> OnStaminaChanged;

    private Coroutine _shieldCoroutine;

    private void Awake()
    {
        playerVFXController = GetComponent<PlayerVFXController>();
    }

    public override void OnStartClient()
    {
        health.Value = maxHealth;
        mana.Value = maxMana;
        stamina.Value = maxStamina;
        hasActiveShield.Value = false;
        health.OnChange += OnHealthChange;
        mana.OnChange += OnManaChange;
        stamina.OnChange += OnStaminaChange;
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
        if (!IsOwner) return;
        OnHealthChanged?.Invoke(next, maxHealth);
    }

    private void OnManaChange(float prev, float next, bool asServer)
    {
        if (!IsOwner) return;
        OnManaChanged?.Invoke(next, maxMana);
    }

    private void OnStaminaChange(float prev, float next, bool asServer)
    {
        if (!IsOwner) return;
        OnStaminaChanged?.Invoke(next, maxStamina);
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
        ReceiveDamageServer(-10f);
    }

    public void UpdateMana(float amount)
    {
        if (!IsOwner) return;
        UpdateManaServer(amount);
    }

    public void UpdateStamina(float amount)
    {
        if (!IsOwner) return;
        UpdateStaminaServer(amount);
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
        if (health.Value + amount > maxHealth)
        {
            health.Value = maxHealth; return;
        }
        if(health.Value + amount < 0)
        {
            health.Value = 0f; return;
        }
        health.Value += amount;
        ParticleSpawn();
    }

    [ServerRpc]
    private void UpdateManaServer(float amount)
    {
        if (mana.Value + amount > maxMana)
        {
            mana.Value = maxMana; return;
        }
        if(mana.Value + amount < 0)
        {
            mana.Value = 0f; return;
        }
        mana.Value += amount;
    }

    [ServerRpc]
    private void UpdateStaminaServer(float amount)
    {
        if (stamina.Value + amount > maxStamina)
        {
            stamina.Value = maxStamina; return;
        }
        if(stamina.Value + amount < 0)
        {
            stamina.Value = 0f; return;
        }
        stamina.Value += amount;
    }
}
