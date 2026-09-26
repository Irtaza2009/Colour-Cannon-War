using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;


public class MultiplayerManager : NetworkBehaviour
{
   private ISession currentSession;
   [SerializeField] private MultiplayerUI multiplayerUI;
   [SerializeField] private GameObject multiplayerMenu;

   private async void Start()
    {
        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("Unity Services Initialised");
            Debug.Log("Player ID:" + AuthenticationService.Instance.PlayerId);
    }
    catch (System.Exception e)
    {
        Debug.LogError("Failed to initialize Unity Services: " + e);
    }
}

private void CheckPlayersConnected()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
        return;
        }

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        Debug.Log("Players connected: " + playerCount);

        if (playerCount == 2)
        {
            StartGame();
        }
    }

private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Client connected: " + clientId);
        CheckPlayersConnected();
    }




private void StartGame()
    {
        Debug.Log("Both players connected. Start now Oloo");

        HideMenuClientRpc();
    }

[ClientRpc]
private void HideMenuClientRpc()
    {
        if (multiplayerMenu != null)
        {
            multiplayerMenu.SetActive(false);
        }
    }



public async void CreateRoom()
    {
        try
        {
            var options = new SessionOptions
            {
                MaxPlayers = 2
            }.WithRelayNetwork();

            currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            Debug.Log("Room created!");
            Debug.Log("Room code: " + currentSession.Code);

            if (multiplayerUI != null)
            {
                multiplayerUI.ShowRoomCode(currentSession.Code);
            }
            else
            {
                Debug.LogError("MultiplayerUI is Oloo!");
            }

            NetworkManager.Singleton.StartHost();

            Debug.Log("NGO Host Started!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to create room: " + e);
        }
    }

    public async void JoinRoom(string roomCode)
    {
        try
        {
            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(roomCode);

            Debug.Log("Joined room!");
            Debug.Log("Session ID: " + currentSession.Id);

            NetworkManager.Singleton.StartClient();

            Debug.Log("NGO Client Started!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to join room: " + e);
        }
    }
}