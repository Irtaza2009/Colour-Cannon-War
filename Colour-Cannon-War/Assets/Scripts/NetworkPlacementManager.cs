using Unity.Netcode;
using UnityEngine;


public class NetworkPlacementManager : NetworkBehaviour
{
    [SerializeField] private GameObject[] placeablePrefabs;
    [SerializeField] private Vector2 gridOrigin = new Vector2(-3.8f, -1.6f);
    [SerializeField, Min(0.01f)] private float cellSize = 1.6f;
    [SerializeField, Min(1)] private int gridDepth = 11;
    [SerializeField, Min(1)] private int territoryDepth = 5;

    public int GetPrefabIndex(GameObject prefab)
    {
        for (int i = 0; i < placeablePrefabs.Length; i++)
        {
            if (placeablePrefabs[i] == prefab)
            {
                return i;
            }
        }
        return -1;
    }
    public void RequestPlacement(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        if (!IsSpawned)
        {
            return;
        }
        PlaceObjectServerRpc(prefabIndex, position, rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceObjectServerRpc(
        int prefabIndex, 
        Vector3 position, 
        Quaternion rotation,
        ServerRpcParams rpcParams = default)
    {
        if (prefabIndex < 0 || prefabIndex >= placeablePrefabs.Length)
        {
            return;
        }

        NetworkPlayer.Team senderTeam = rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId
            ? NetworkPlayer.Team.Blue
            : NetworkPlayer.Team.Red;

        if (!IsPositionInTerritory(position, senderTeam))
        {
            return;
        }

        GameObject prefab = placeablePrefabs[prefabIndex];

        if (prefab == null)
        {
            return;
        }

        GameObject placedObject = Instantiate(prefab, position, rotation);

        NetworkObject networkObject = placedObject.GetComponent<NetworkObject>();

        if(networkObject == null)
        {
            Debug.LogError(
                prefab.name + " is missing a NetworkObject component!"
            );
            Destroy(placedObject);
            return;
        }

        networkObject.Spawn();

    }

    private bool IsPositionInTerritory(Vector3 position, NetworkPlayer.Team team)
    {
        int zIndex = Mathf.RoundToInt((position.z - gridOrigin.y) / cellSize);
        int clampedTerritoryDepth = Mathf.Clamp(territoryDepth, 1, gridDepth);

        if (zIndex < 0 || zIndex >= gridDepth)
        {
            return false;
        }

        return team == NetworkPlayer.Team.Blue
            ? zIndex < clampedTerritoryDepth
            : zIndex >= gridDepth - clampedTerritoryDepth;
    }

}
