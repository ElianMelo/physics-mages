using FishNet.Managing;
using FishNet.Transporting.UTP;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Paused,
        Moving,
        Drawing
    }

    public GameState currentGameState = GameState.Moving;
    [SerializeField] private NetworkManager _networkManager;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            currentGameState = currentGameState == GameState.Moving ? GameState.Drawing : GameState.Moving;
            SwitchState();
        }
        if(Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            StartHost();
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            StartClient();
        }
    }

    public void StartHost()
    {
        StartServer();
        StartClient();
    }

    public void StartServer()
    {
        _networkManager.ServerManager.StartConnection();
    }

    public void StartClient()
    {
        _networkManager.ClientManager.StartConnection();
    }

    public void SetIPAddress(string text)
    {
        _networkManager.TransportManager.Transport.SetClientAddress(text);
    }

    private void SwitchState()
    {
        switch (currentGameState)   
        {
            case GameState.Paused:
                Cursor.lockState = CursorLockMode.None;
                break;
            case GameState.Moving:
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case GameState.Drawing:
                Cursor.lockState = CursorLockMode.None;
                break;
            default:
                break;
        }
    }

    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Request allocation and join code
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        // Configure transport
        var unityTransport = _networkManager.TransportManager.GetTransport<UnityTransport>();
        unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));

        // Start host
        if (_networkManager.ServerManager.StartConnection()) // Server is successfully started.
        {
            _networkManager.ClientManager.StartConnection(); // You can choose not to call this method. Then only the server will start.
            return joinCode;
        }
        return null;
    }

    public async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        _networkManager.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        return !string.IsNullOrEmpty(joinCode) && _networkManager.ClientManager.StartConnection();
    }
}
