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
        UpdateLabels();
        UpdateScores();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = refreshInterval;
        UpdateLabels();
        UpdateScores();
    }

    private void UpdateLabels()
    {
        if (TeamCameraController.Instance == null || !TeamCameraController.Instance.HasTeam)
        {
            return;
        }

        bool localPlayerIsBlue = TeamCameraController.Instance.CurrentTeam == NetworkPlayer.Team.Blue;
        SetText(blueHeadingText, localPlayerIsBlue ? "You" : "Blue");
        SetText(redHeadingText, localPlayerIsBlue ? "Red" : "You");
    }

    private void UpdateScores()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsSpawned)
        {
            int synchronizedBlueScore = GameManager.Instance.BlueScore;
            int synchronizedRedScore = GameManager.Instance.RedScore;
            SetScoreTexts(synchronizedBlueScore, synchronizedRedScore);
            return;
        }

        if (blueMaterial == null || redMaterial == null)
        {
            return;
        }

        int blueScore = 0;
        int redScore = 0;
        Renderer[] tileRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);

        foreach (Renderer tileRenderer in tileRenderers)
        {
            if (((1 << tileRenderer.gameObject.layer) & tileLayer.value) == 0)
            {
                continue;
            }

            Vector3 tilePosition = tileRenderer.bounds.center;

            if (tilePosition.x < arenaXBounds.x || tilePosition.x > arenaXBounds.y
                || tilePosition.z < arenaZBounds.x || tilePosition.z > arenaZBounds.y)
            {
                continue;
            }

            Material tileMaterial = tileRenderer.sharedMaterial;

            if (tileMaterial == blueMaterial || IsMaterialMatch(tileMaterial, blueMaterial))
            {
                blueScore++;
            }
            else if (tileMaterial == redMaterial || IsMaterialMatch(tileMaterial, redMaterial))
            {
                redScore++;
            }
        }

        SetScoreTexts(blueScore, redScore);
    }

    private void SetScoreTexts(int blueScore, int redScore)
    {
        bool localPlayerIsBlue = TeamCameraController.Instance == null
            || !TeamCameraController.Instance.HasTeam
            || TeamCameraController.Instance.CurrentTeam == NetworkPlayer.Team.Blue;

        SetText(blueScoreText, localPlayerIsBlue ? blueScore.ToString() : redScore.ToString());
        SetText(redScoreText, localPlayerIsBlue ? redScore.ToString() : blueScore.ToString());
    }

    private static bool IsMaterialMatch(Material currentMaterial, Material targetMaterial)
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
