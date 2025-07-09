using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Exclamation : NetworkBehaviour
{
    public GameObject exclamationFX;

    void Start()
    {
        exclamationFX.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (NetworkManager.Singleton.IsListening)
                TriggerExclamationServerRpc();
            else
                StartCoroutine(ExclamationOn());
        }
    }

    public void OnButtonClick()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (NetworkManager.Singleton.IsListening)
            TriggerExclamationServerRpc();
        else
            StartCoroutine(ExclamationOn());
    }

    [ServerRpc]
    void TriggerExclamationServerRpc()
    {
        TriggerExclamationClientRpc();
    }

    [ClientRpc]
    void TriggerExclamationClientRpc()
    {
        StartCoroutine(ExclamationOn());
    }

    IEnumerator ExclamationOn()
    {
        exclamationFX.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        exclamationFX.SetActive(false);
    }
}
