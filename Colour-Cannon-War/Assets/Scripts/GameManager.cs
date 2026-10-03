using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameManager : NetworkBehaviour
{
   public static GameManager Instance;

   [Header("Territory Rows")]
   [SerializeField] private Transform[] blueTerritoryRows;
   [SerializeField] private Transform[] redTerritoryRows;

   [Header("Tile Materials")]
   [SerializeField] private Material blueMaterial;
   [SerializeField] private Material redMaterial;

   [Header("Game Over UI")]
   [SerializeField] private GameObject gameOverPanel;
   [SerializeField] private TMP_Text winnerText;

   private NetworkVariable<bool> gameOver = new NetworkVariable<bool>(false);

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
    }

    public void TileWasHit(Collider tileCollider)
    {
        if (!IsServer || gameOver.Value)
        {
            return;
        }

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (IsEntireTerritoryCaptured(blueTerritoryRows, redMaterial))
        {
            EndGame(1);
            return;
        }

        if (IsEntireTerritoryCaptured(redTerritoryRows, blueMaterial))
        {
            EndGame(0);
        }
    }

    private bool IsEntireTerritoryCaptured(
            Transform[] rows,
            Material targetMaterial)
    {
        if (rows == null || rows.Length == 0 || targetMaterial == null)
        {
            return false;
        }

        foreach (Transform row in rows)
        {
            if (row == null)
            {
                return false;
            }

            foreach (Transform tile in row)
            {
                Renderer tileRenderer = tile.GetComponent<Renderer>();

                if (tileRenderer == null)
                {
                    tileRenderer = tile.GetComponentInChildren<Renderer>();
                }

                if (tileRenderer == null)
                {
                    return false;
                }

                Material currentMaterial = tileRenderer.sharedMaterial;
                
                if (currentMaterial != targetMaterial &&
                    !IsMaterialMatch(currentMaterial, targetMaterial))                                    
                {
                    return false;
                }

            }
        }
        return true;
    }

    private static bool IsMaterialMatch(
        Material currentMaterial,
         Material targetMaterial)
    {
        if (currentMaterial == null || targetMaterial == null)
        {
            return false;
        }
        return currentMaterial.name.StartsWith(targetMaterial.name);
    }
    
    private void EndGame(int winningTeam)
    {
        if (!IsServer || gameOver.Value)
        {
            return;
        }

        gameOver.Value = true;

        ShowWinnerClientRpc(winningTeam);
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(int winningTeam)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        if (winnerText != null)
        {
            winnerText.text = winningTeam == 0 ? "Blue Wins!" : "Red Wins!";
        }
    }

    private void OnGameOverChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log("Game Over");
        }
    }

}
