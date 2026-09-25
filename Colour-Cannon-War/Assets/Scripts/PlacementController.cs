using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Camera placementCamera;
    [SerializeField] private LayerMask tileLayer = ~0;
    [SerializeField] private float tileTopOffset = 0.02f;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-3.8f, -1.6f);
    [SerializeField, Min(0.01f)] private float cellSize = 1.6f;
    [SerializeField, Min(1)] private int gridWidth = 10;
    [SerializeField, Min(1)] private int gridDepth = 11;
    [SerializeField] private Color validPreviewColor = new Color(0.65f, 0.65f, 0.65f, 0.65f);
    [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.25f, 0.25f, 0.65f);

    private readonly List<GameObject> placedObjects = new();
    private GameObject selectedPrefab;
    private GameObject previewObject;
    private Renderer[] previewRenderers;
    private Vector3 previewPosition;
    private bool previewIsValid;

    private void Awake()
    {
        if (placementCamera == null)
        {
            placementCamera = Camera.main;
        }
    }

    private void Update()
    {
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

    public void SelectPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        selectedPrefab = prefab;
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
        previewPosition = previewObject.transform.position;
        previewIsValid = !IsPositionOccupied();
        SetPreviewColor(previewIsValid ? validPreviewColor : invalidPreviewColor);
    }

    private bool TryGetGridPosition(Vector3 hitPosition, out Vector3 snappedPosition)
    {
        int xIndex = Mathf.RoundToInt((hitPosition.x - gridOrigin.x) / cellSize);
        int zIndex = Mathf.RoundToInt((hitPosition.z - gridOrigin.y) / cellSize);

        if (xIndex < 0 || xIndex >= gridWidth || zIndex < 0 || zIndex >= gridDepth)
        {
            snappedPosition = default;
            return false;
        }

        snappedPosition = new Vector3(
            gridOrigin.x + xIndex * cellSize,
            hitPosition.y,
            gridOrigin.y + zIndex * cellSize);
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
        Bounds previewBounds = GetPreviewBounds();

        foreach (GameObject placedObject in placedObjects)
        {
            if (placedObject == null)
            {
                continue;
            }

            Bounds placedBounds = GetObjectBounds(placedObject);

            if (previewBounds.Intersects(placedBounds))
            {
                return true;
            }
        }

        return false;
    }

    private GameObject PlaceSelectedObject()
    {
        GameObject placedObject = Instantiate(selectedPrefab, previewPosition, previewObject.transform.rotation);
        placedObjects.Add(placedObject);
        DestroyPreview();
        selectedPrefab = null;
        return placedObject;
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

    private static Bounds GetObjectBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
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
