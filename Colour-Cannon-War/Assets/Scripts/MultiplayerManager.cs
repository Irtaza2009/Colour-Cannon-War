using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MultiplayerManager : MonoBehaviour
{
    private const int SessionOperationTimeoutSeconds = 30;

   private ISession currentSession;
   [SerializeField] private MultiplayerUI multiplayerUI;
    [SerializeField] private LoadingUI loadingUI;
    [SerializeField] private string gameplaySceneName = "GameScene";
    private bool gameplaySceneLoading;
    private bool sessionOperationInProgress;
    private Task initializationTask;

       private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

   private async void Start()
    {
        initializationTask = InitializeServices();
        await initializationTask;
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
        if (gameplaySceneLoading || string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            return;
        }

        gameplaySceneLoading = true;
        SetLoading("Loading game...");
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private void SetLoading(string message)
    {
        if (loadingUI != null)
        {
            loadingUI.SetLoading(true, message);
        }
    }



public async void CreateRoom()
    {
        if (sessionOperationInProgress)
        {
            return;
        }

        sessionOperationInProgress = true;
        SetLoading("Creating room...");

        try
        {
            await WaitForInitialization();

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

            SetLoading(false);
            Debug.Log("Relay session created; the Multiplayer Services Netcode handler will start the host.");
        }
        catch (System.Exception e)
        {
            SetLoading(false);
            Debug.LogError("Failed to create room: " + e);
        }
        finally
        {
            sessionOperationInProgress = false;
        }
    }

    public async void JoinRoom(string roomCode)
    {
        if (sessionOperationInProgress)
        {
            return;
        }

        sessionOperationInProgress = true;
        SetLoading("Joining room...");

        try
        {
            await WaitForInitialization();

            Task<ISession> joinTask = MultiplayerService.Instance.JoinSessionByCodeAsync(roomCode);
            Task completedTask = await Task.WhenAny(
                joinTask,
                Task.Delay(System.TimeSpan.FromSeconds(SessionOperationTimeoutSeconds)));

            if (completedTask != joinTask)
            {
                throw new System.TimeoutException(
                    "Joining the room timed out. Check that the host is still connected and try again.");
            }

            currentSession = await joinTask;

            Debug.Log("Joined room!");
            Debug.Log("Session ID: " + currentSession.Id);

            Debug.Log("Relay session joined; the Multiplayer Services Netcode handler will start the client.");
        }
        catch (System.Exception e)
        {
            SetLoading(false);
            Debug.LogError("Failed to join room: " + e);
        }
        finally
        {
            sessionOperationInProgress = false;
        }
    }

    private async Task WaitForInitialization()
    {
        if (initializationTask == null)
        {
            initializationTask = InitializeServices();
        }

        await initializationTask;
    }

    private void SetLoading(bool isLoading)
    {
        if (loadingUI != null)
        {
            loadingUI.SetLoading(isLoading);
        }
    }
}