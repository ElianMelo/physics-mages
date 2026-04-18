using FishNet.Managing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
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
        if(Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            StartHost();
        }
        if (Keyboard.current.digit0Key.wasPressedThisFrame)
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
}
