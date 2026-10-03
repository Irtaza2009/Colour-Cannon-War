using UnityEngine;
using Unity.Netcode;

public class ProjectileController : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float disappearDelay = 1f;

    private Material projectileMaterial;
    private bool hasLanded;
    private readonly NetworkVariable<Vector3> networkPosition = new();
    private readonly NetworkVariable<Color32> networkColor = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            Rigidbody body = GetComponent<Rigidbody>();

            if (body != null)
            {
                body.isKinematic = true;
            }
        }

        networkPosition.OnValueChanged += OnPositionChanged;
        networkColor.OnValueChanged += OnColorChanged;
        ApplyNetworkColor(networkColor.Value);
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            networkPosition.Value = transform.position;
        }
    }

    public void SetMaterial(Material material)
    {
        projectileMaterial = material;

        if (IsServer && material != null)
        {
            networkColor.Value = material.color;
        }

        ApplyMaterialToSelf();
    }

    public void DespawnAfter(float delay)
    {
        if (IsServer)
        {
            StartCoroutine(DespawnAfterCoroutine(delay));
        }
        else if (!IsSpawned)
        {
            Destroy(gameObject, delay);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        if (hasLanded)
        {
            return;
        }

        hasLanded = true;
        ApplyMaterialToTarget(collision.collider);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TileWasHit(collision.collider);
        }

        ContactPoint contact = collision.GetContact(0);
        if (IsSpawned)
        {
            ApplyTileColorClientRpc(contact.point, contact.normal, networkColor.Value);
        }

        DespawnAfter(disappearDelay);
    }

    [ClientRpc]
    private void ApplyTileColorClientRpc(Vector3 hitPoint, Vector3 surfaceNormal, Color32 color)
    {
        if (IsServer)
        {
            return;
        }

        if (TryGetRendererAtImpact(hitPoint, surfaceNormal, out Renderer targetRenderer))
        {
            targetRenderer.material.color = color;
        }
    }

    private System.Collections.IEnumerator DespawnAfterCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void OnPositionChanged(Vector3 previousPosition, Vector3 newPosition)
    {
        if (!IsServer)
        {
            transform.position = newPosition;
        }
    }

    private void OnColorChanged(Color32 previousColor, Color32 newColor)
    {
        ApplyNetworkColor(newColor);
    }

    private void ApplyNetworkColor(Color32 color)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.material.color = color;
        }
    }

    private void ApplyMaterialToSelf()
    {
        if (projectileMaterial == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.material = projectileMaterial;
        }
    }

    private void ApplyMaterialToTarget(Collider targetCollider)
    {
        if (projectileMaterial == null)
        {
            return;
        }

        Renderer targetRenderer = targetCollider.GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            targetRenderer = targetCollider.GetComponentInParent<Renderer>();
        }

        if (targetRenderer != null)
        {
            targetRenderer.material = projectileMaterial;
        }
    }

    private static bool TryGetRendererAtImpact(
        Vector3 hitPoint,
        Vector3 surfaceNormal,
        out Renderer targetRenderer)
    {
        Ray impactRay = new Ray(hitPoint + surfaceNormal * 0.2f, -surfaceNormal);

        if (Physics.Raycast(impactRay, out RaycastHit raycastHit, 1f))
        {
            targetRenderer = raycastHit.collider.GetComponent<Renderer>();

            if (targetRenderer == null)
            {
                targetRenderer = raycastHit.collider.GetComponentInParent<Renderer>();
            }

            if (targetRenderer != null)
            {
                return true;
            }
        }

        Collider[] nearbyColliders = Physics.OverlapSphere(hitPoint, 0.3f);

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            targetRenderer = nearbyCollider.GetComponent<Renderer>();

            if (targetRenderer == null)
            {
                targetRenderer = nearbyCollider.GetComponentInParent<Renderer>();
            }

            if (targetRenderer != null)
            {
                return true;
            }
        }

        targetRenderer = null;
        return false;
    }
}
