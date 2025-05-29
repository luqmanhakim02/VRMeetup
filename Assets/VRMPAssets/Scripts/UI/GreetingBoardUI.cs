using TMPro;
using UnityEngine;
using XRMultiplayer;
using CustomMultiplayer;

public class GreetingBoardUI : MonoBehaviour
{
    [SerializeField] TMP_Text m_RoomNameText;
    [SerializeField] TMP_Text m_RoomCodeText;


    private void OnEnable()
    {
        NetworkGameManager.Connected.Subscribe(ConnectedToGame);
        NetworkGameManager.ConnectedRoomName.Subscribe(UpdateRoomName);
    }

    private void OnDisable()
    {
        NetworkGameManager.Connected.Unsubscribe(ConnectedToGame);
        NetworkGameManager.ConnectedRoomName.Unsubscribe(UpdateRoomName);
    }

    void ConnectedToGame(bool connected)
    {
        if (connected)
        {
            m_RoomNameText.text = NetworkGameManager.ConnectedRoomName.Value;
            m_RoomCodeText.text = NetworkGameManager.ConnectedRoomCode;
        }
    }

    void UpdateRoomName(string roomName)
    {
        m_RoomNameText.text = roomName;
    }
}
