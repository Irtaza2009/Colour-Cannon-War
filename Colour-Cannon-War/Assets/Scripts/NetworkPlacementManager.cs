using Unity.Netcode;
using UnityEngine;


public class NetworkPlacementManager : NetworkBehaviour
{
    [SerializeField] private GameObject[] placeablePrefabs;

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
