using UnityEngine;

public class CannonController : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private Rigidbody projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Material projectileMaterial;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
    [SerializeField, Range(0f, 89f)] private float launchAngle = 18f;
    [SerializeField, Min(0f)] private float projectileLifetime = 10f;

    [Header("Shot Randomness")]
    [SerializeField, Range(0f, 20f)] private float horizontalSpread = 4f;
    [SerializeField, Range(0f, 20f)] private float launchAngleSpread = 2f;

    [Header("Cylinder Rotation")]
    [SerializeField] private bool rotateCylinder;
    [SerializeField] private Transform cylinder;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [SerializeField, Min(0.1f)] private float rotationSpeed = 40f;

    [Header("Automatic Fire")]
    [SerializeField] private bool fireOnStart = true;
    [SerializeField, Min(0.01f)] private float reloadTime = 2f;

    private float reloadTimer;
    private Quaternion cylinderStartingRotation;
    private float rotationStartTime;

    private void Start()
    {
        if (cylinder != null)
        {
            cylinderStartingRotation = cylinder.localRotation;
        }

        rotationStartTime = Time.time;
        reloadTimer = fireOnStart ? 0f : reloadTime;
    }

    private void Update()
    {
        RotateCylinder();

        reloadTimer -= Time.deltaTime;

        if (reloadTimer > 0f)
        {
            return;
        }

        Fire();
        reloadTimer = reloadTime;
    }

    private void RotateCylinder()
    {
        if (!rotateCylinder || cylinder == null || rotationAxis.sqrMagnitude < 0.001f)
        {
            return;
        }

        float cycleDuration = 120f / rotationSpeed;
        float cycleTime = Mathf.Repeat(Time.time - rotationStartTime, cycleDuration);
        float elapsedDegrees = cycleTime * rotationSpeed;
        float angle;

        if (elapsedDegrees <= 30f)
        {
            angle = elapsedDegrees;
        }
        else if (elapsedDegrees <= 90f)
        {
            angle = 30f - (elapsedDegrees - 30f);
        }
        else
        {
            angle = elapsedDegrees - 120f;
        }

        cylinder.localRotation = cylinderStartingRotation * Quaternion.AngleAxis(angle, rotationAxis.normalized);
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Transform firingPoint = muzzle != null ? muzzle : transform;
        Rigidbody projectile = Instantiate(projectilePrefab, firingPoint.position, firingPoint.rotation);
        ProjectileController projectileController = projectile.GetComponent<ProjectileController>();

        if (projectileController != null)
        {
            projectileController.SetMaterial(projectileMaterial);
        }

        float randomHorizontalOffset = Random.Range(-horizontalSpread, horizontalSpread);
        float randomLaunchAngle = launchAngle + Random.Range(-launchAngleSpread, launchAngleSpread);
        randomLaunchAngle = Mathf.Clamp(randomLaunchAngle, 0f, 89f);
        Vector3 randomForward = Quaternion.AngleAxis(randomHorizontalOffset, firingPoint.up) * firingPoint.forward;
        float angleInRadians = randomLaunchAngle * Mathf.Deg2Rad;
        Vector3 forwardVelocity = randomForward * (projectileSpeed * Mathf.Cos(angleInRadians));
        Vector3 upwardVelocity = firingPoint.up * (projectileSpeed * Mathf.Sin(angleInRadians));
        projectile.linearVelocity = forwardVelocity + upwardVelocity;

        if (projectileLifetime > 0f)
        {
            Destroy(projectile.gameObject, projectileLifetime);
        }
    }
}
