using UnityEngine;

public class CannonController : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private Rigidbody projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Range(0f, 89f)] private float launchAngle = 35f;
    [SerializeField, Min(0f)] private float projectileLifetime = 10f;

    [Header("Automatic Fire")]
    [SerializeField] private bool fireOnStart = true;
    [SerializeField, Min(0.01f)] private float reloadTime = 2f;

    private float reloadTimer;

    private void Start()
    {
        reloadTimer = fireOnStart ? 0f : reloadTime;
    }

    private void Update()
    {
        reloadTimer -= Time.deltaTime;

        if (reloadTimer > 0f)
        {
            return;
        }

        Fire();
        reloadTimer = reloadTime;
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Transform firingPoint = muzzle != null ? muzzle : transform;
        Rigidbody projectile = Instantiate(projectilePrefab, firingPoint.position, firingPoint.rotation);

        float angleInRadians = launchAngle * Mathf.Deg2Rad;
        Vector3 forwardVelocity = firingPoint.forward * (projectileSpeed * Mathf.Cos(angleInRadians));
        Vector3 upwardVelocity = firingPoint.up * (projectileSpeed * Mathf.Sin(angleInRadians));
        projectile.linearVelocity = forwardVelocity + upwardVelocity;

        if (projectileLifetime > 0f)
        {
            Destroy(projectile.gameObject, projectileLifetime);
        }
    }
}
