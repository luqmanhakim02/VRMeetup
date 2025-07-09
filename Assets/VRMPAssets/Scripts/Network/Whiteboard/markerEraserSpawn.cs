using System;
using Unity.Netcode;
using UnityEngine;

public class markerEraserSpawn : NetworkBehaviour
{
    public GameObject marker;         // Reference to the marker prefab
    public GameObject eraser;         // Reference to the eraser prefab (new field)
    public bool DestroyWithSpawner;   // Flag to destroy spawned objects with the spawner

    private GameObject m_MarkerInstance;
    private GameObject m_EraserInstance;
    private NetworkObject m_SpawnedNetworkMarker;
    private NetworkObject m_SpawnedNetworkEraser;

    public override void OnNetworkSpawn()
    {
        // Only the server spawns, clients will disable this component on their side
        enabled = IsServer;
        if (!enabled || marker == null)
        {
            return;
        }

        try
        {
            // Instantiate the Marker and Eraser prefabs
            m_MarkerInstance = Instantiate(marker);

            // Apply the spawner's position and rotation to both Marker and Eraser instances
            m_MarkerInstance.transform.position = transform.position;
            m_MarkerInstance.transform.rotation = transform.rotation;

            // Get the NetworkObjects and Spawn them
            m_SpawnedNetworkMarker = m_MarkerInstance.GetComponent<NetworkObject>();
           
            m_SpawnedNetworkMarker.Spawn();
            

            if (eraser != null)
            {
                m_EraserInstance = Instantiate(eraser);

                m_EraserInstance.transform.position = transform.position;
                m_EraserInstance.transform.rotation = transform.rotation;

                m_SpawnedNetworkEraser = m_EraserInstance.GetComponent<NetworkObject>();

                m_SpawnedNetworkEraser.Spawn();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error spawning marker and eraser: {e}");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && DestroyWithSpawner)
        {
            if (m_SpawnedNetworkMarker != null && m_SpawnedNetworkMarker.IsSpawned)
            {
                m_SpawnedNetworkMarker.Despawn();
            }

            if (m_SpawnedNetworkEraser != null && m_SpawnedNetworkEraser.IsSpawned)
            {
                m_SpawnedNetworkEraser.Despawn();
            }
        }
        base.OnNetworkDespawn();
    }
}
