using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Question : NetworkBehaviour
{
    public GameObject questionFX;

    void Start()
    {
        questionFX.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            if (NetworkManager.Singleton.IsListening)
                TriggerQuestionServerRpc();
            else
                StartCoroutine(QuestionOn());
        }
    }

    public void OnButtonClick()
    {
        if (!IsOwner && NetworkManager.Singleton.IsListening) return;

        if (NetworkManager.Singleton.IsListening)
            TriggerQuestionServerRpc();
        else
            StartCoroutine(QuestionOn());
    }

    [ServerRpc]
    void TriggerQuestionServerRpc()
    {
        TriggerQuestionClientRpc();
    }

    [ClientRpc]
    void TriggerQuestionClientRpc()
    {
        StartCoroutine(QuestionOn());
    }

    IEnumerator QuestionOn()
    {
        questionFX.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        questionFX.SetActive(false);
    }
}
