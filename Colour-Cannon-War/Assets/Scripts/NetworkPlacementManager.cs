using Unity.Netcode;
using System.Collections.Generic;
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

    // Kept by every peer for immediate preview feedback. The server also uses this
    // set as the authoritative guard against two clients claiming the same cell.
    private readonly HashSet<PlacementCell> occupiedCells = new();

    private readonly struct PlacementCell
    {
        public readonly NetworkPlayer.Team Team;
        public readonly int X;
        public readonly int Z;

        public PlacementCell(NetworkPlayer.Team team, int x, int z)
        {
            Team = team;
            X = x;
            Z = z;
        }
    }

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

        if (!TryGetPlacementCell(position, senderTeam, out PlacementCell cell))
        {
            return;
        }

        // Do this before charging coins. This must live on the server because a
        // client-side preview alone cannot prevent simultaneous placement requests.
        if (occupiedCells.Contains(cell))
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
        occupiedCells.Add(cell);
        MarkCellOccupiedClientRpc(senderTeam, cell.X, cell.Z);

    }

    public bool IsCellOccupied(Vector3 position, NetworkPlayer.Team team)
    {
        return TryGetPlacementCell(position, team, out PlacementCell cell)
            && occupiedCells.Contains(cell);
    }

    private bool TryGetPlacementCell(Vector3 position, NetworkPlayer.Team team, out PlacementCell cell)
    {
        float zStart = team == NetworkPlayer.Team.Blue
            ? bluePlacementZStart
            : redPlacementZStart;
        int xIndex = Mathf.RoundToInt((position.x - placementXStart) / placementCellSize);
        int zIndex = Mathf.RoundToInt((position.z - zStart) / placementCellSize);

        if (xIndex < 0 || xIndex >= placementWidth || zIndex < 0 || zIndex >= placementRows)
        {
            cell = default;
            return false;
        }

        float expectedX = placementXStart + xIndex * placementCellSize;
        float expectedZ = zStart + zIndex * placementCellSize;
        if (Mathf.Abs(position.x - expectedX) > placementCellSize * 0.3f
            || Mathf.Abs(position.z - expectedZ) > placementCellSize * 0.3f)
        {
            cell = default;
            return false;
        }

        cell = new PlacementCell(team, xIndex, zIndex);
        return true;
    }

    [ClientRpc]
    private void MarkCellOccupiedClientRpc(NetworkPlayer.Team team, int xIndex, int zIndex)
    {
        occupiedCells.Add(new PlacementCell(team, xIndex, zIndex));
    }

}
