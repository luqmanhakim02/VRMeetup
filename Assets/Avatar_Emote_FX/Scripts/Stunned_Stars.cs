using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Stunned_Stars : NetworkBehaviour
{
    public GameObject stunnedStarsFX;

    void Start()
    {
        stunnedStarsFX.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (NetworkManager.Singleton.IsListening)
                TriggerStunnedServerRpc();
            else
                StartCoroutine(Stunned());
        }
    }

    public void OnButtonClick()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (NetworkManager.Singleton.IsListening)
            TriggerStunnedServerRpc();
        else
            StartCoroutine(Stunned());
    }

    [ServerRpc]
    void TriggerStunnedServerRpc()
    {
        TriggerStunnedClientRpc();
    }

    [ClientRpc]
    void TriggerStunnedClientRpc()
    {
        StartCoroutine(Stunned());
    }

    IEnumerator Stunned()
    {
        stunnedStarsFX.SetActive(true);
        yield return new WaitForSeconds(6.0f);
        stunnedStarsFX.SetActive(false);
    }
}
