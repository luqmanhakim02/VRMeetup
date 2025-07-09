using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CustomMultiplayer;

namespace XRMultiplayer
{
    /// <summary>
    /// A simple example of how to setup a player appearance menu and utilize the bindable variables.
    /// </summary>
    public class PlayerAppearanceMenu : MonoBehaviour
    {
        [SerializeField] Color[] m_PlayerColors;
        [SerializeField] TMP_InputField m_PlayerNameInputField;
        [SerializeField] Image m_PlayerIconColor;


        void Awake()
        {
            NetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
            Debug.Log("SINIIIII PLAYER NAMA: " + NetworkGameManager.LocalPlayerName.Value);
            NetworkGameManager.LocalPlayerColor.Subscribe(SetPlayerColor);
        }

        void Start()
        {
            SetPlayerName(NetworkGameManager.LocalPlayerName.Value);
            SetPlayerColor(NetworkGameManager.LocalPlayerColor.Value);
        }

        void OnDestroy()
        {
            NetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
            NetworkGameManager.LocalPlayerColor.Unsubscribe(SetPlayerColor);
        }

        /// <summary>
        /// Use this to set the player's name so it triggers the bindable variable
        /// </summary>
        /// <param name="text"></param>
        public void SubmitNewPlayerName(string text)
        {
            NetworkGameManager.LocalPlayerName.Value = text;
        }

        /// <summary>
        /// Use this to set the player's color so it triggers the bindable variable
        /// </summary>
        /// <param name="text"></param>
        public void SetRandomColor()
        {
            List<Color> availableColors = new(m_PlayerColors);
            if (availableColors.Remove(NetworkGameManager.LocalPlayerColor.Value))
            {

                NetworkGameManager.LocalPlayerColor.Value = availableColors[Random.Range(0, availableColors.Count)];
            }
            else
            {
                NetworkGameManager.LocalPlayerColor.Value = m_PlayerColors[Random.Range(0, m_PlayerColors.Length)];
            }
        }

        void SetPlayerName(string newName)
        {
            m_PlayerNameInputField.text = newName;
        }

        void SetPlayerColor(Color color)
        {
            m_PlayerIconColor.color = color;
        }
    }
}
