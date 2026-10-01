using TMPro;
using UnityEngine;

public class TileScoreUI : MonoBehaviour
{
    [Header("Tile Detection")]
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material redMaterial;
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
