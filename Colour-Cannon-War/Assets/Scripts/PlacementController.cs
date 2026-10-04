using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private LayerMask tileLayer = ~0;
    [SerializeField] private float tileTopOffset = 0.02f;
    [SerializeField] private float placementXStart = -3.8f;
    [SerializeField, Min(0.01f)] private float placementCellSize = 1.6f;
    [SerializeField, Min(1)] private int placementWidth = 9;
    [SerializeField, Min(1)] private int placementRows = 3;
    [SerializeField] private float bluePlacementZStart = -14f;
    [SerializeField] private float redPlacementZStart = 10.8f;
    [SerializeField] private float mortarZOffset = -0.3f;
    [SerializeField] private float redMortarZOffset = 0.3f;
    [SerializeField] private Color validPreviewColor = new Color(0.65f, 0.65f, 0.65f, 0.65f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.25f, 0.25f, 0.65f);

    private GameObject selectedPrefab;
    [SerializeField] private NetworkPlacementManager networkPlacementManager;
    private GameObject previewObject;
    private Renderer[] previewRenderers;
    private Vector3 previewPosition;
    private bool previewIsValid;
    private int selectedPurchaseCost;

    private void Awake()
    {
        if (placementCamera == null)
        {
            placementCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdatePlacementCamera();

        if (selectedPrefab == null || placementCamera == null)
        {
            return;
        }

        UpdatePreview();

        if (WasPointerPressed() && !IsPointerOverUi() && previewIsValid)
        {
            PlaceSelectedObject();
        }
    }

    private void UpdatePlacementCamera()
    {
        if (TeamCameraController.Instance != null)
        {
            Camera teamCamera = TeamCameraController.Instance.GetCameraForCurrentTeam();

            if (teamCamera != null)
            {
                placementCamera = teamCamera;
                return;
            }
        }

        if (placementCamera == null)
        {
            placementCamera = Camera.main;
        }
    }

    public void SelectPrefab(GameObject prefab, int purchaseCost)
    {
        if (prefab == null)
        {
            return;
        }

        selectedPrefab = prefab;
        selectedPurchaseCost = purchaseCost;
        CreatePreview();
    }

    public void CancelPlacement()
    {
        selectedPrefab = null;
        DestroyPreview();
    }

    private void CreatePreview()
    {
        DestroyPreview();
        previewObject = Instantiate(selectedPrefab);
        previewObject.name = selectedPrefab.name + " Preview";
        previewRenderers = previewObject.GetComponentsInChildren<Renderer>(true);

        foreach (MonoBehaviour behaviour in previewObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            behaviour.enabled = false;
        }

        foreach (Collider collider in previewObject.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody body in previewObject.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        SetPreviewColor(validPreviewColor);
        previewObject.SetActive(false);
    }

    private void UpdatePreview()
    {
        if (!TryGetPointerPosition(out Vector2 pointerPosition))
        {
            previewObject.SetActive(false);
            previewIsValid = false;
            return;
        }

        Ray pointerRay = placementCamera.ScreenPointToRay(pointerPosition);

        if (!Physics.Raycast(pointerRay, out RaycastHit hit, Mathf.Infinity, tileLayer))
        {
            previewObject.SetActive(false);
            previewIsValid = false;
            return;
        }

        if (!TryGetGridPosition(hit.point, out Vector3 snappedPosition))
        {
            previewObject.SetActive(false);
            previewIsValid = false;
            return;
        }

        previewObject.SetActive(true);
        previewObject.transform.position = snappedPosition;
        previewObject.transform.rotation = selectedPrefab.transform.rotation;
        AlignPreviewToTileTop(hit.collider);
        ApplyPrefabOffset();
        previewPosition = previewObject.transform.position;
        previewIsValid = !IsPositionOccupied();
        SetPreviewColor(previewIsValid ? validPreviewColor : invalidPreviewColor);
    }

    private void ApplyPrefabOffset()
    {
        TeslaTowerController teslaTowerController = selectedPrefab.GetComponent<TeslaTowerController>();

        if (teslaTowerController != null)
        {
            Vector3 position = previewObject.transform.position;
            previewObject.transform.position = new Vector3(position.x, teslaTowerController.PlacementY, position.z);
            return;
        }

        CannonController cannonController = selectedPrefab.GetComponent<CannonController>();

        if (cannonController != null && cannonController.UsesExplodingProjectile())
        {
            float offset = mortarZOffset;

            if (TeamCameraController.Instance != null
                && TeamCameraController.Instance.HasTeam
                && TeamCameraController.Instance.CurrentTeam == NetworkPlayer.Team.Red)
            {
                offset = redMortarZOffset;
            }

            previewObject.transform.position += Vector3.forward * offset;
        }
    }

    private bool TryGetGridPosition(Vector3 hitPosition, out Vector3 snappedPosition)
    {
        if (TeamCameraController.Instance == null || !TeamCameraController.Instance.HasTeam)
        {
            snappedPosition = default;
            return false;
        }

        NetworkPlayer.Team team = TeamCameraController.Instance.CurrentTeam;
        float zStart = team == NetworkPlayer.Team.Blue
            ? bluePlacementZStart
            : redPlacementZStart;
        int xIndex = Mathf.RoundToInt((hitPosition.x - placementXStart) / placementCellSize);
        int zIndex = Mathf.RoundToInt((hitPosition.z - zStart) / placementCellSize);

        if (xIndex < 0 || xIndex >= placementWidth || zIndex < 0 || zIndex >= placementRows)
        {
            snappedPosition = default;
            return false;
        }

        snappedPosition = new Vector3(
            placementXStart + xIndex * placementCellSize,
            hitPosition.y,
            zStart + zIndex * placementCellSize);
        return true;
    }

    private void AlignPreviewToTileTop(Collider tileCollider)
    {
        Bounds bounds = GetPreviewBounds();
        float verticalOffset = tileCollider.bounds.max.y + tileTopOffset - bounds.min.y;
        previewObject.transform.position += Vector3.up * verticalOffset;
    }

    private Bounds GetPreviewBounds()
    {
        Bounds bounds = new Bounds(previewObject.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in previewRenderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private bool IsPositionOccupied()
    {
        if (networkPlacementManager == null
            || TeamCameraController.Instance == null
            || !TeamCameraController.Instance.HasTeam)
        {
            return false;
        }

        return networkPlacementManager.IsCellOccupied(
            previewPosition,
            TeamCameraController.Instance.CurrentTeam);
    }

    private GameObject PlaceSelectedObject()
    {
        if (GameManager.Instance != null
            && GameManager.Instance.IsSpawned
            && GameManager.Instance.GetLocalCoins() < selectedPurchaseCost)
        {
            CancelPlacement();
            return null;
        }

        int prefabIndex = networkPlacementManager.GetPrefabIndex(selectedPrefab);

        if (prefabIndex == -1)
        {
            Debug.LogError("Selected prefab is not registered in NetworkPlacementManager!");
            return null;
        }
        networkPlacementManager.RequestPlacement(
            prefabIndex, 
            previewPosition, 
            selectedPrefab.transform.rotation);

            DestroyPreview();
            selectedPrefab = null;

            return null;
    }

    private void SetPreviewColor(Color color)
    {
        if (previewRenderers == null)
        {
            return;
        }

        foreach (Renderer renderer in previewRenderers)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void DestroyPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        previewObject = null;
        previewRenderers = null;
        previewIsValid = false;
    }

    private bool WasPointerPressed()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }

        return Input.GetMouseButtonDown(0);
    }

    private bool TryGetPointerPosition(out Vector2 pointerPosition)
    {
        if (Input.touchCount > 0)
        {
            pointerPosition = Input.GetTouch(0).position;
            return true;
        }

        pointerPosition = Input.mousePosition;
        return true;
    }

    private bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
