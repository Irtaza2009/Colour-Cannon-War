using UnityEngine;

public class PlacementInventoryButton : MonoBehaviour
{
    [SerializeField] private PlacementController placementController;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject bluePrefab;
    [SerializeField] private GameObject redPrefab;
    [SerializeField] private GameObject blueImage;
    [SerializeField] private GameObject redImage;
    [SerializeField, Min(0)] private int weaponCost = 10;

    private NetworkPlayer.Team displayedTeam;
    private bool hasDisplayedTeam;

    private void Update()
    {
        if (TeamCameraController.Instance == null || !TeamCameraController.Instance.HasTeam)
        {
            return;
        }

        NetworkPlayer.Team currentTeam = TeamCameraController.Instance.CurrentTeam;

        if (!hasDisplayedTeam || displayedTeam != currentTeam)
        {
            SetTeamImage(currentTeam);
        }
    }

    public void SelectItem()
    {
        if (placementController == null)
        {
            return;
        }

        if (TeamCameraController.Instance != null && TeamCameraController.Instance.HasTeam)
        {
            SetTeamImage(TeamCameraController.Instance.CurrentTeam);
        }

        GameObject prefabToPlace = itemPrefab;

        if (TeamCameraController.Instance != null && TeamCameraController.Instance.HasTeam)
        {
            prefabToPlace = TeamCameraController.Instance.CurrentTeam == NetworkPlayer.Team.Blue
                ? bluePrefab
                : redPrefab;
        }

        if (prefabToPlace != null)
        {
            if (GameManager.Instance != null
                && GameManager.Instance.IsSpawned
                && GameManager.Instance.GetLocalCoins() < weaponCost)
            {
                return;
            }

            placementController.SelectPrefab(prefabToPlace, weaponCost);
        }
    }

    private void SetTeamImage(NetworkPlayer.Team team)
    {
        displayedTeam = team;
        hasDisplayedTeam = true;

        if (blueImage != null)
        {
            blueImage.SetActive(team == NetworkPlayer.Team.Blue);
        }

        if (redImage != null)
        {
            redImage.SetActive(team == NetworkPlayer.Team.Red);
        }
    }
}
