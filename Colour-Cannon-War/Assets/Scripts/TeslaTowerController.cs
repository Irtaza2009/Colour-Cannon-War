using Unity.Netcode;
using UnityEngine;

public class TeslaTowerController : NetworkBehaviour
{
    [Header("Placement")]
    [SerializeField] private float placementY = 0.2f;

    [Header("Targeting")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private LayerMask tileLayer;
    [SerializeField] private Material teamTileMaterial;
    [SerializeField, Min(0.1f)] private float range = 30f;

    [Header("Firing")]
    [SerializeField, Min(0.01f)] private float reloadTime = 3f;
    [SerializeField, Min(0f)] private float reloadVariance = 0.2f;
    [SerializeField] private bool fireOnStart = true;

    [Header("Beam Visual")]
    [SerializeField] private LineRenderer beamRenderer;
    [SerializeField, Min(0.01f)] private float beamDuration = 0.15f;
    [SerializeField, Min(0.001f)] private float beamWidth = 0.08f;

    private float reloadTimer;
    private Coroutine beamRoutine;

    public float PlacementY => placementY;

    private void Start()
    {
        reloadTimer = fireOnStart ? 0f : GetNextReloadTime();

        if (beamRenderer != null)
        {
            beamRenderer.useWorldSpace = true;
            beamRenderer.positionCount = 2;
            beamRenderer.widthMultiplier = beamWidth;
            beamRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if (IsNetworkSessionActive() && (!IsServer || !IsSpawned))
        {
            return;
        }

        reloadTimer -= Time.deltaTime;

        if (reloadTimer > 0f)
        {
            return;
        }

        FireAtNearestUncolouredTile();
        reloadTimer = GetNextReloadTime();
    }

    private void FireAtNearestUncolouredTile()
    {
        if (teamTileMaterial == null)
        {
            Debug.LogWarning("Tesla Tower needs a team tile material.", this);
            return;
        }

        Transform firingPoint = muzzle != null ? muzzle : transform;
        Renderer target = FindNearestUncolouredTile(firingPoint.position);

        if (target == null)
        {
            return;
        }

        bool changedColor = !IsMaterialMatch(target.sharedMaterial, teamTileMaterial);
        target.material = teamTileMaterial;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TileWasHit(null, teamTileMaterial, changedColor);
        }

        Vector3 targetPosition = target.bounds.center;
        Color beamColor = teamTileMaterial.color;
        ShowBeam(firingPoint.position, targetPosition, beamColor);

        if (IsNetworkSessionActive() && IsSpawned)
        {
            FireBeamClientRpc(targetPosition, beamColor);
            PlayZapSoundClientRPC();
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayZapSound();
        }
    }

    private float GetNextReloadTime()
    {
        float minimumReloadTime = Mathf.Max(0.01f, reloadTime - reloadVariance);
        return Random.Range(minimumReloadTime, reloadTime + reloadVariance);
    }

    private Renderer FindNearestUncolouredTile(Vector3 origin)
    {
        Renderer nearestTile = null;
        float nearestDistanceSquared = range * range;

        foreach (Renderer candidate in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (((1 << candidate.gameObject.layer) & tileLayer.value) == 0
                || IsMaterialMatch(candidate.sharedMaterial, teamTileMaterial))
            {
                continue;
            }

            float distanceSquared = (candidate.bounds.center - origin).sqrMagnitude;

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestTile = candidate;
            }
        }

        return nearestTile;
    }

    [ClientRpc]
    private void FireBeamClientRpc(Vector3 targetPosition, Color32 color)
    {
        // The server already changed its tile and displayed its beam.
        if (IsServer)
        {
            return;
        }

        Transform firingPoint = muzzle != null ? muzzle : transform;
        Renderer target = FindNearestTileAt(targetPosition);

        if (target != null)
        {
            target.material.color = color;
        }

        ShowBeam(firingPoint.position, targetPosition, color);


    }

    private Renderer FindNearestTileAt(Vector3 position)
    {
        Renderer nearestTile = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (Renderer candidate in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (((1 << candidate.gameObject.layer) & tileLayer.value) == 0)
            {
                continue;
            }

            float distanceSquared = (candidate.bounds.center - position).sqrMagnitude;

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestTile = candidate;
            }
        }

        return nearestTile;
    }

    private void ShowBeam(Vector3 startPosition, Vector3 endPosition, Color color)
    {
        if (beamRenderer == null)
        {
            return;
        }

        if (beamRoutine != null)
        {
            StopCoroutine(beamRoutine);
        }

        beamRenderer.startColor = color;
        beamRenderer.endColor = color;
        beamRenderer.widthMultiplier = beamWidth;
        beamRenderer.SetPosition(0, startPosition);
        beamRenderer.SetPosition(1, endPosition);
        beamRenderer.enabled = true;
        beamRoutine = StartCoroutine(HideBeamAfterDelay());
    }

    private System.Collections.IEnumerator HideBeamAfterDelay()
    {
        yield return new WaitForSeconds(beamDuration);

        if (beamRenderer != null)
        {
            beamRenderer.enabled = false;
        }

        beamRoutine = null;
    }

    private static bool IsMaterialMatch(Material currentMaterial, Material targetMaterial)
    {
        return currentMaterial != null
            && targetMaterial != null
            && (currentMaterial == targetMaterial
                || currentMaterial.name.StartsWith(targetMaterial.name));
    }

    private static bool IsNetworkSessionActive()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    [ClientRpc]
    private void PlayZapSoundClientRPC()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayZapSound();
        }
    }
}
