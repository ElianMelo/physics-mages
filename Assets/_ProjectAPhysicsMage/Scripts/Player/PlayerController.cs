using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private GameObject playerHealthCanvas;
    [SerializeField] private Image playerHealthImage;
    [SerializeField] private float maxHealth;    
    private readonly SyncVar<float> playerHealth = new SyncVar<float>(new SyncTypeSettings(1f));
    private readonly SyncVar<bool> hasActiveShield = new SyncVar<bool>(new SyncTypeSettings(0.1f));
    private readonly SyncVar<MagicElement> shieldElement = new SyncVar<MagicElement>(new SyncTypeSettings(0.1f));
    private Coroutine _shieldCoroutine;
    public PlayerVFXController playerVFXController { get; private set; }

    private void Awake()
    {
        playerVFXController = GetComponent<PlayerVFXController>();
    }

    public override void OnStartClient()
    {
        playerHealth.Value = maxHealth;
        hasActiveShield.Value = false;
        playerHealth.OnChange += OnHealthChange;
        if (!IsOwner) return;
        playerHealthCanvas.SetActive(false);
    }

    private void OnHealthChange(float prev, float next, bool asServer)
    {
        playerHealthImage.fillAmount = next / maxHealth;
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

    [ServerRpc(RunLocally=true)]
    private void ReceiveDamageServer(int amount)
    {
        playerHealth.Value -= amount;
    }
}
