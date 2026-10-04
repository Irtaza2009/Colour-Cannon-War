using TMPro;
using UnityEngine;

public class TileScoreUI : MonoBehaviour
{
    [Header("Tile Detection")]
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Vector2 arenaXBounds = new Vector2(-4.2f, 9.4f);
    [SerializeField] private Vector2 arenaZBounds = new Vector2(-6.4f, 6.4f);
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

    [Header("Blue Side UI")]
    [SerializeField] private TMP_Text blueHeadingText;
    [SerializeField] private TMP_Text blueScoreText;

    [Header("Red Side UI")]
    [SerializeField] private TMP_Text redHeadingText;
    [SerializeField] private TMP_Text redScoreText;

    private float refreshTimer;

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = refreshInterval;
        RefreshUI();
    }

    private void RefreshUI()
    {
        UpdateScores();
    }

    private void UpdateScores()
    {
        int blueScore;
        int redScore;

        // Prefer the synchronized network scores once the GameManager exists.
        if (GameManager.Instance != null && GameManager.Instance.IsSpawned)
        {
            blueScore = GameManager.Instance.BlueScore;
            redScore = GameManager.Instance.RedScore;
        }
        else
        {
            CalculateTileScores(out blueScore, out redScore);
        }

        UpdateDisplay(blueScore, redScore);
    }

    private void CalculateTileScores(out int blueScore, out int redScore)
    {
        blueScore = 0;
        redScore = 0;

        if (blueMaterial == null || redMaterial == null)
        {
            return;
        }

        Renderer[] tileRenderers =
            FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);

        foreach (Renderer tileRenderer in tileRenderers)
        {
            if (((1 << tileRenderer.gameObject.layer) & tileLayer.value) == 0)
            {
                continue;
            }

            Vector3 tilePosition = tileRenderer.bounds.center;

            if (tilePosition.x < arenaXBounds.x ||
                tilePosition.x > arenaXBounds.y ||
                tilePosition.z < arenaZBounds.x ||
                tilePosition.z > arenaZBounds.y)
            {
                continue;
            }

            Material tileMaterial = tileRenderer.sharedMaterial;

            if (tileMaterial == blueMaterial ||
                IsMaterialMatch(tileMaterial, blueMaterial))
            {
                blueScore++;
            }
            else if (tileMaterial == redMaterial ||
                     IsMaterialMatch(tileMaterial, redMaterial))
            {
                redScore++;
            }
        }
    }

    private void UpdateDisplay(int blueScore, int redScore)
    {
        bool localPlayerIsBlue = true;

        if (TeamCameraController.Instance != null &&
            TeamCameraController.Instance.HasTeam)
        {
            localPlayerIsBlue =
                TeamCameraController.Instance.CurrentTeam == NetworkPlayer.Team.Blue;
        }

        if (localPlayerIsBlue)
        {
            // Blue player sees:
            // You    = blue
            // Red    = red

            SetText(blueHeadingText, "You");
            SetText(blueScoreText, blueScore.ToString());

            SetText(redHeadingText, "Red");
            SetText(redScoreText, redScore.ToString());
        }
        else
        {
            // Red player sees:
            // Red    = blue's opponent
            // You    = red

            SetText(blueHeadingText, "Red");
            SetText(blueScoreText, blueScore.ToString());

            SetText(redHeadingText, "You");
            SetText(redScoreText, redScore.ToString());
        }
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

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}