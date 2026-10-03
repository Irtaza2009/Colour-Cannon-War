using UnityEngine;
using Unity.Netcode;

public class ExplodingProjectile : NetworkBehaviour
{
    [Header("Explosion")]
    [SerializeField, Min(0.1f)] private float fuseTime = 8f;
    [SerializeField] private Rigidbody smallerProjectilePrefab;
    [SerializeField, Range(3, 5)] private int minimumProjectileCount = 3;
    [SerializeField, Range(3, 5)] private int maximumProjectileCount = 5;
    [SerializeField, Min(0f)] private float splitSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float sidewaysSpread = 0.2f;
    [SerializeField, Range(0f, 1f)] private float parentVelocityInfluence = 0.5f;
    [SerializeField, Min(0f)] private float spawnSeparation = 0.05f;

    private Material projectileMaterial;
    private float fuseTimer;
    private bool hasExploded;
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

    private void Start()
    {
        fuseTimer = fuseTime;
    }

    private void Update()
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        fuseTimer -= Time.deltaTime;

        if (fuseTimer <= 0f)
        {
            Explode();
        }
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

    private void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;
        int projectileCount = Random.Range(minimumProjectileCount, maximumProjectileCount + 1);
        Rigidbody[] spawnedProjectiles = new Rigidbody[projectileCount];
        Rigidbody parentBody = GetComponent<Rigidbody>();
        Vector3 parentVelocity = parentBody != null ? parentBody.linearVelocity : Vector3.down;

        if (smallerProjectilePrefab != null)
        {
            for (int index = 0; index < projectileCount; index++)
            {
                spawnedProjectiles[index] = SpawnSmallerProjectile(parentVelocity);
            }

            IgnoreExplosionCollisions(spawnedProjectiles);
        }

        if (IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Rigidbody SpawnSmallerProjectile(Vector3 parentVelocity)
    {
        Vector3 travelDirection = parentVelocity.sqrMagnitude > 0.01f
            ? parentVelocity.normalized
            : Vector3.down;
        travelDirection.y = Mathf.Min(travelDirection.y, -0.15f);
        travelDirection.Normalize();

        Vector3 sidewaysOffset = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f));
        Vector3 direction = (travelDirection + sidewaysOffset * sidewaysSpread).normalized;
        Vector3 velocity = parentVelocity * parentVelocityInfluence + direction * splitSpeed;
        velocity.y = Mathf.Min(velocity.y, -0.1f);

        Rigidbody smallerProjectile = Instantiate(
            smallerProjectilePrefab,
            transform.position + direction * spawnSeparation,
            Random.rotation);

        ProjectileController projectileController = smallerProjectile.GetComponent<ProjectileController>();

        if (IsSpawned)
        {
            NetworkObject networkObject = smallerProjectile.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("The smaller projectile prefab needs a NetworkObject component.");
                Destroy(smallerProjectile.gameObject);
                return null;
            }

            networkObject.Spawn();
        }

        if (projectileController != null)
        {
            projectileController.SetMaterial(projectileMaterial);
        }

        smallerProjectile.linearVelocity = velocity;
        return smallerProjectile;
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

    private void IgnoreExplosionCollisions(Rigidbody[] spawnedProjectiles)
    {
        Collider[] explosionColliders = GetComponentsInChildren<Collider>();

        for (int index = 0; index < spawnedProjectiles.Length; index++)
        {
            if (spawnedProjectiles[index] == null)
            {
                continue;
            }

            Collider[] childColliders = spawnedProjectiles[index].GetComponentsInChildren<Collider>();

            foreach (Collider explosionCollider in explosionColliders)
            {
                foreach (Collider childCollider in childColliders)
                {
                    Physics.IgnoreCollision(explosionCollider, childCollider);
                }
            }

            for (int otherIndex = index + 1; otherIndex < spawnedProjectiles.Length; otherIndex++)
            {
                if (spawnedProjectiles[otherIndex] == null)
                {
                    continue;
                }

                Collider[] otherChildColliders = spawnedProjectiles[otherIndex].GetComponentsInChildren<Collider>();

                foreach (Collider childCollider in childColliders)
                {
                    foreach (Collider otherChildCollider in otherChildColliders)
                    {
                        Physics.IgnoreCollision(childCollider, otherChildCollider);
                    }
                }
            }
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
}
