using Unity.Netcode;
using UnityEngine;


public class NetworkPlacementManager : NetworkBehaviour
{
    [SerializeField] private GameObject[] placeablePrefabs;
    [SerializeField, Min(0)] private int[] placeablePrefabCosts;
    [SerializeField] private float placementXStart = -3.8f;
    [SerializeField, Min(0.01f)] private float placementCellSize = 1.6f;
    [SerializeField, Min(1)] private int placementWidth = 9;
    [SerializeField, Min(1)] private int placementRows = 3;
    [SerializeField] private float bluePlacementZStart = -14f;
    [SerializeField] private float redPlacementZStart = 10.8f;

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

        if (!IsPositionInPlacementZone(position, senderTeam))
        {
            return;
        }

        GameObject prefab = placeablePrefabs[prefabIndex];

        if (prefab == null)
        {
            return;
        }

        NetworkObject prefabNetworkObject = prefab.GetComponent<NetworkObject>();

        if (prefabNetworkObject == null)
        {
            return;
        }

        if (placeablePrefabCosts == null || prefabIndex >= placeablePrefabCosts.Length)
        {
            Debug.LogError("Assign a cost for every placeable prefab in NetworkPlacementManager.");
            return;
        }

        int purchaseCost = placeablePrefabCosts[prefabIndex];

        if (GameManager.Instance == null
            || !GameManager.Instance.TrySpendCoins(senderTeam, purchaseCost))
        {
            return;
        }

        GameObject placedObject = Instantiate(prefab, position, rotation);

        placedObject.GetComponent<NetworkObject>().Spawn();

    }

    private bool IsPositionInPlacementZone(Vector3 position, NetworkPlayer.Team team)
    {
        float zStart = team == NetworkPlayer.Team.Blue
            ? bluePlacementZStart
            : redPlacementZStart;
        int xIndex = Mathf.RoundToInt((position.x - placementXStart) / placementCellSize);
        int zIndex = Mathf.RoundToInt((position.z - zStart) / placementCellSize);

        if (xIndex < 0 || xIndex >= placementWidth || zIndex < 0 || zIndex >= placementRows)
        {
            return false;
        }

        float expectedX = placementXStart + xIndex * placementCellSize;
        float expectedZ = zStart + zIndex * placementCellSize;
        return Mathf.Abs(position.x - expectedX) <= placementCellSize * 0.3f
            && Mathf.Abs(position.z - expectedZ) <= placementCellSize * 0.3f;
    }

}
