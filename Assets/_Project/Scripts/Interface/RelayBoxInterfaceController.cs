using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelayBoxInterfaceController : MonoBehaviour
{
    [SerializeField] private Button startHost;
    [SerializeField] private Button startClient;
    [SerializeField] private TMP_InputField relayCode;

    void Start()
    {
        startHost.onClick.AddListener(OnStartHost);
        startClient.onClick.AddListener(OnStartClient);
    }

    private void OnStartHost()
    {
        AwaitStartHost();
    }

    private async void AwaitStartHost()
    {
        
        string result = await GameManager.Instance.StartHostWithRelay(3, "dtls");
        relayCode.text = result;
    }
    private void OnStartClient()
    {
        _ = GameManager.Instance.StartClientWithRelay(relayCode.text, "dtls");
    }
}
