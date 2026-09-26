using Unity.Netcode;
using UnityEngine;


public class NetworkPlacementManager : NetworkBehaviour
{
    [SerializeField] private GameObject[] placeablePrefabs;

    public void RequestPlacement(int prefabIndex, Vector3 position, Quaternion rotation)
    {
        if (!IsSpawned)
        {
            return;
        }
        PlaceObjectServerRpc(prefabIndex, position, rotation);
    }

    [ServerRpc]
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

}
