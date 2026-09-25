using UnityEngine;

public class PlacementInventoryButton : MonoBehaviour
{
    [SerializeField] private PlacementController placementController;
    [SerializeField] private GameObject itemPrefab;

    public void SelectItem()
    {
        if (placementController != null && itemPrefab != null)
        {
            placementController.SelectPrefab(itemPrefab);
        }
    }
}
