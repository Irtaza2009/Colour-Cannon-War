using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;


public class MultiplayerManager : MonoBehaviour
{
   private ISession currentSession;
   [SerializeField] private MultiplayerUI multiplayerUI;
    [SerializeField] private LoadingUI loadingUI;
    [SerializeField] private string gameplaySceneName = "GameScene";
    private bool gameplaySceneLoading;
    private bool sessionOperationInProgress;
    private bool networkCallbacksSubscribed;
    private Task initializationTask;

       private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

   private async void Start()
    {
        SubscribeToNetworkCallbacks();
        initializationTask = InitializeServices();
        await initializationTask;
    }

    private async Task InitializeServices()
    {
        try
        {
            InitializationOptions options = new InitializationOptions();

#if UNITY_WEBGL && !UNITY_EDITOR
            // Browser tabs on the same itch.io origin share local storage. Give
            // each tab a separate anonymous-auth profile so host and joining
            // player never reuse the same UGS player identity.
            string webGlProfile = "webgl-" + System.Guid.NewGuid().ToString("N").Substring(0, 24);
            options.SetProfile(webGlProfile);
#endif

            await UnityServices.InitializeAsync(options);

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
        SubscribeToNetworkCallbacks();
    }

private void OnDisable()
    {
        if (networkCallbacksSubscribed && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        networkCallbacksSubscribed = false;
    }

    private void SubscribeToNetworkCallbacks()
    {
        if (networkCallbacksSubscribed || NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        networkCallbacksSubscribed = true;
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
            ConfigureTransportForCurrentPlatform();

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
            ConfigureTransportForCurrentPlatform();

            // Joining also starts the Relay/Netcode connection. Do not impose a
            // local timeout: cancelling our await does not cancel that network
            // operation, which leaves the player in the lobby and makes retries
            // fail with "player is already a member of the lobby".
            currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(roomCode);
            SetLoading(false);

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

    private static void ConfigureTransportForCurrentPlatform()
    {
        UnityTransport transport = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.GetComponent<UnityTransport>()
            : null;

        if (transport == null)
        {
            throw new System.InvalidOperationException(
                "NetworkManager needs a UnityTransport component to create or join a Relay room.");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // Browsers can only connect to Relay through secure WebSockets (WSS).
        transport.UseWebSockets = true;
#else
        // Native players use Relay's default UDP/DTLS connection. Leaving this
        // enabled causes the WebSocket-driver/Relay-protocol mismatch in the log.
        transport.UseWebSockets = false;
#endif
    }

    private void SetLoading(bool isLoading)
    {
        if (loadingUI != null)
        {
            loadingUI.SetLoading(isLoading);
        }
    }
}
