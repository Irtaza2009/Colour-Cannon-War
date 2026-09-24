using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float disappearDelay = 1f;

    private Material projectileMaterial;
    private bool hasLanded;

    public void SetMaterial(Material material)
    {
        projectileMaterial = material;
        ApplyMaterialToSelf();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasLanded)
        {
            return;
        }

        hasLanded = true;
        ApplyMaterialToTarget(collision.collider);
        Destroy(gameObject, disappearDelay);
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
}
