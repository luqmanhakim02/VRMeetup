using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Light_Bulb : NetworkBehaviour
{
    public GameObject bulbFX;

    void Start()
    {
        bulbFX.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (Input.GetKeyDown(KeyCode.X))
        {
            if (NetworkManager.Singleton.IsListening)
            {
                TriggerBulbServerRpc();
            }
            else
            {
                StartCoroutine(BulbOn()); // Local fallback if network is not active
            }
        }
    }

    public void OnButtonClick()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (NetworkManager.Singleton.IsListening)
        {
            TriggerBulbServerRpc();
        }
        else
        {
            StartCoroutine(BulbOn()); // Local fallback
        }
    }

    [ServerRpc]
    void TriggerBulbServerRpc()
    {
        TriggerBulbClientRpc();
    }

    [ClientRpc]
    void TriggerBulbClientRpc()
    {
        StartCoroutine(BulbOn());
    }

    IEnumerator BulbOn()
    {
        bulbFX.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        bulbFX.SetActive(false);
    }
}
