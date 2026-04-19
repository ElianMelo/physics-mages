using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Image playerHealthImage;
    [SerializeField] private float maxHealth;    
    private readonly SyncVar<float> playerHealth = new SyncVar<float>(new SyncTypeSettings(1f));

    public override void OnStartClient()
    {
        playerHealth.Value = maxHealth;
        playerHealth.OnChange += OnHealthChange;
        if (!IsOwner) return;
    }

    private void OnHealthChange(float prev, float next, bool asServer)
    {
        playerHealthImage.fillAmount = next / maxHealth;
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
