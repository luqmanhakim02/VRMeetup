using UnityEngine;

public class SyncGameObjectState : MonoBehaviour
{

    [SerializeField] GameObject objectsToSync1;
    [SerializeField] GameObject objectsToSync2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (objectsToSync1.activeSelf)
        {
            objectsToSync2.SetActive(true);
        }
        else
        {
            objectsToSync2.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (objectsToSync1.activeSelf)
        {
            objectsToSync2.SetActive(true);
        }
        else
        {
            objectsToSync2.SetActive(false);
        }
    }
}
