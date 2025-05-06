using UnityEngine;

namespace XRMultiplayer
{
    public class PlayerOptionsManager : MonoBehaviour
    {
        [SerializeField] GameObject playerOptionsMenu;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (playerOptionsMenu != null)
                {
                    bool isActive = playerOptionsMenu.activeSelf;
                    playerOptionsMenu.SetActive(!isActive);
                }
            }
        }
    }
}
