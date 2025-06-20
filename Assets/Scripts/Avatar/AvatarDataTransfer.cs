using UnityEngine;
using UnityEngine.UI; // For the Button component
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using System.Linq;

namespace ReadyPlayerMe.XR
{
    public class AvatarDataTransfer : MonoBehaviour
    {
        [SerializeField] private GameObject networkAvatarPrefab; // Field for the network avatar
        [SerializeField] private GameObject customizedAvatar; // Field for the customized avatar
        [SerializeField] private Button transferButton; // The button that triggers the transfer

        private AvatarManager avatarManager;

        private void Start()
        {
            avatarManager = new AvatarManager(); // Adjust as needed based on your setup.

            // Ensure the button is assigned and add the onClick listener
            if (transferButton != null)
            {
                transferButton.onClick.AddListener(OnTransferButtonClicked);
            }
            else
            {
                Debug.LogError("Transfer Button is not assigned!");
            }
        }

        // Button click handler
        private void OnTransferButtonClicked()
        {
            if (customizedAvatar == null || networkAvatarPrefab == null)
            {
                Debug.LogError("Either customizedAvatar or networkAvatar is null.");
                return;
            }

            // Call the transfer function when the button is clicked
            TransferToNetworkAvatar(customizedAvatar, networkAvatarPrefab);
        }

        // Transfer the customized avatar data (meshes, textures, etc.) to the network avatar
        public void TransferToNetworkAvatar(GameObject customizedAvatar, GameObject networkAvatar)
        {
            if (networkAvatar == null || customizedAvatar == null)
            {
                Debug.LogError("Network Avatar or Customized Avatar is null.");
                return;
            }

            // Transfer all meshes and assets (such as textures, materials) from the customized avatar to the network avatar
            TransferMeshes(customizedAvatar, networkAvatar);

        }

        private void TransferMeshes(GameObject customizedAvatar, GameObject networkAvatarPrefab)
        {
            GameObject networkAvatar = Instantiate(networkAvatarPrefab);

            // Assuming the customized avatar has the same structure as the network avatar
            var customizedMeshes = customizedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            var networkMeshes = networkAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var networkMesh in networkMeshes)
            {
                // Find the corresponding mesh in the customized avatar and transfer its properties
                var correspondingMesh = customizedMeshes.FirstOrDefault(mesh => mesh.name == networkMesh.name);
                if (correspondingMesh != null)
                {
                    networkMesh.sharedMesh = correspondingMesh.sharedMesh; // Transfer the mesh
                    networkMesh.materials = correspondingMesh.materials; // Transfer materials
                }
            }

            // Optional: Update network avatar properties, such as metadata
            UpdateNetworkAvatarProperties(customizedAvatar, networkAvatar);
        }

        private void UpdateNetworkAvatarProperties(GameObject customizedAvatar, GameObject networkAvatar)
        {
            // Here, you can update properties such as gender, body type, or other metadata
            var customizedAvatarMetadata = customizedAvatar.GetComponent<AvatarData>();
            if (customizedAvatarMetadata != null)
            {
                // Example: Update network avatar properties
                var networkAvatarMetadata = networkAvatar.GetComponent<AvatarData>();
                networkAvatarMetadata.AvatarId = customizedAvatarMetadata.AvatarId;
                networkAvatarMetadata.AvatarMetadata = customizedAvatarMetadata.AvatarMetadata;
            }
        }
    }
}
