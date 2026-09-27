using UnityEngine;

public class PlacementInventoryButton : MonoBehaviour
{
    [SerializeField] private PlacementController placementController;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private GameObject bluePrefab;
    [SerializeField] private GameObject redPrefab;

    public void SelectItem()
    {
        if (placementController == null)
        {
            return;
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
            placementController.SelectPrefab(prefabToPlace);
        }
    }
}
