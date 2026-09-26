using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;


public class MultiplayerManager : MonoBehaviour
{
   private ISession currentSession;
   [SerializeField] private MultiplayerUI multiplayerUI;

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
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to join room: " + e);
        }
    }
}