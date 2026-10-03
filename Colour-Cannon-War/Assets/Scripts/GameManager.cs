using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Arena")]
    [SerializeField] private LayerMask arenaTileLayer;
    [SerializeField] private Vector2 arenaXBounds = new Vector2(-4.2f, 9.4f);
    [SerializeField] private Vector2 arenaZBounds = new Vector2(-6.4f, 6.4f);
    [SerializeField, Min(1f)] private float gameDuration = 90f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField, Min(0)] private int startingCoins = 50;
    [SerializeField, Min(0)] private int coinsPerTile = 2;

    [Header("Tile Materials")]
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material redMaterial;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text winnerText;

    private readonly NetworkVariable<bool> gameOver = new();
    private readonly NetworkVariable<float> timeRemaining = new();
    private readonly NetworkVariable<int> blueScore = new();
    private readonly NetworkVariable<int> redScore = new();
    private readonly NetworkVariable<int> blueCoins = new();
    private readonly NetworkVariable<int> redCoins = new();

    public int BlueScore => blueScore.Value;
    public int RedScore => redScore.Value;

    public int GetLocalCoins()
    {
        if (TeamCameraController.Instance == null || !TeamCameraController.Instance.HasTeam)
        {
            return 0;
        }

        return TeamCameraController.Instance.CurrentTeam == NetworkPlayer.Team.Blue
            ? blueCoins.Value
            : redCoins.Value;
    }

    private void Awake()
    {
        Instance = this;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        gameOver.OnValueChanged += OnGameOverChanged;
        timeRemaining.OnValueChanged += OnTimeRemainingChanged;
        blueCoins.OnValueChanged += OnCoinsChanged;
        redCoins.OnValueChanged += OnCoinsChanged;

        if (IsServer)
        {
            timeRemaining.Value = gameDuration;
            blueCoins.Value = startingCoins;
            redCoins.Value = startingCoins;
        }

        UpdateTimerText(timeRemaining.Value);
        UpdateCoinsText();
    }

    public override void OnNetworkDespawn()
    {
        gameOver.OnValueChanged -= OnGameOverChanged;
        timeRemaining.OnValueChanged -= OnTimeRemainingChanged;
        blueCoins.OnValueChanged -= OnCoinsChanged;
        redCoins.OnValueChanged -= OnCoinsChanged;
    }

    private void Update()
    {
        if (!IsServer || gameOver.Value)
        {
            return;
        }

        timeRemaining.Value = Mathf.Max(0f, timeRemaining.Value - Time.deltaTime);

        if (timeRemaining.Value <= 0f)
        {
            EndGame();
        }
    }

    public void TileWasHit(Collider tileCollider, Material newMaterial, bool changedColor)
    {
        if (!IsServer || gameOver.Value)
        {
            return;
        }

        UpdateScores();

        if (changedColor)
        {
            AwardCoins(newMaterial);
        }

        if (timeRemaining.Value <= 0f)
        {
            EndGame();
        }
    }

    public bool TrySpendCoins(NetworkPlayer.Team team, int amount)
    {
        if (!IsServer || amount < 0)
        {
            return false;
        }

        if (team == NetworkPlayer.Team.Blue)
        {
            if (blueCoins.Value < amount)
            {
                return false;
            }

            blueCoins.Value -= amount;
        }
        else
        {
            if (redCoins.Value < amount)
            {
                return false;
            }

            redCoins.Value -= amount;
        }

        return true;
    }

    private void AwardCoins(Material newMaterial)
    {
        if (IsMaterialMatch(newMaterial, blueMaterial))
        {
            blueCoins.Value += coinsPerTile;
        }
        else if (IsMaterialMatch(newMaterial, redMaterial))
        {
            redCoins.Value += coinsPerTile;
        }
    }

    private void EndGame()
    {
        if (!IsServer || gameOver.Value)
        {
            return;
        }

        gameOver.Value = true;
        UpdateScores();
        int winningTeam = blueScore.Value == redScore.Value
            ? -1
            : blueScore.Value > redScore.Value ? 0 : 1;
        ShowWinnerClientRpc(winningTeam, blueScore.Value, redScore.Value);
    }

    private void UpdateScores()
    {
        CountArenaTiles(out int currentBlueScore, out int currentRedScore);
        blueScore.Value = currentBlueScore;
        redScore.Value = currentRedScore;
    }

    private void CountArenaTiles(out int blueScore, out int redScore)
    {
        blueScore = 0;
        redScore = 0;
        Renderer[] tileRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);

        foreach (Renderer tileRenderer in tileRenderers)
        {
            if (((1 << tileRenderer.gameObject.layer) & arenaTileLayer.value) == 0)
            {
                continue;
            }

            Vector3 tilePosition = tileRenderer.bounds.center;

            if (tilePosition.x < arenaXBounds.x || tilePosition.x > arenaXBounds.y
                || tilePosition.z < arenaZBounds.x || tilePosition.z > arenaZBounds.y)
            {
                continue;
            }

            Material currentMaterial = tileRenderer.sharedMaterial;

            if (IsMaterialMatch(currentMaterial, blueMaterial))
            {
                blueScore++;
            }
            else if (IsMaterialMatch(currentMaterial, redMaterial))
            {
                redScore++;
            }
        }
    }

    private static bool IsMaterialMatch(Material currentMaterial, Material targetMaterial)
    {
        if (currentMaterial == null || targetMaterial == null)
        {
            return false;
        }

        return currentMaterial == targetMaterial
            || currentMaterial.name.StartsWith(targetMaterial.name);
    }

    private void OnTimeRemainingChanged(float oldValue, float newValue)
    {
        UpdateTimerText(newValue);
    }

    private void UpdateCoinsText()
    {
        if (coinsText != null)
        {
            coinsText.text = "Coins: " + GetLocalCoins().ToString();
        }
    }

    private void OnCoinsChanged(int oldValue, int newValue)
    {
        UpdateCoinsText();
    }

    private void UpdateTimerText(float seconds)
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(seconds).ToString() + "s";
        }
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(int winningTeam, int blueScore, int redScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (winnerText == null)
        {
            return;
        }

        if (winningTeam < 0)
        {
            winnerText.text = "Draw!";
            winnerText.color = Color.white;
            return;
        }
        
        if (winningTeam == 0)
        {
            winnerText.text = "Blue Wins!";
        }
        else
        {
            winnerText.text = "Red Wins!";
        }
        Material winningMaterial = winningTeam == 0 ? blueMaterial : redMaterial;

        if (winningMaterial != null)
        {
            winnerText.color = winningMaterial.color;
        }
    }

    private void OnGameOverChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("Game Over");
        }
    }

    public void ReturnToMenuButton()
    {
        SceneManager.LoadScene("MenuScene");
    }
}
