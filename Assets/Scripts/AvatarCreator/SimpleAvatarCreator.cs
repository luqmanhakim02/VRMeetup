using System.Collections.Generic;
using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ReadyPlayerMe.XR
{
    public class SimpleAvatarCreator : MonoBehaviour
    {
        [SerializeField] private AvatarConfig avatarConfig;
        [SerializeField] private GameObject loading;

        [SerializeField] private GameObject panelTemplateSelection;
        [SerializeField] private GameObject panelElements;
        [SerializeField] private TemplateSelectionElement templateSelectionElement;

        [SerializeField] private PanelManager mainPanelManager;
        [SerializeField] private List<AssetSelectionUI> assetSelectionElementUis;

        [SerializeField] private UnityEvent<AvatarProperties> onTemplateSelected;

        [SerializeField] private Button saveButton;  // Reference to the save button

        private AvatarManager avatarManager;

        private OutfitGender gender = OutfitGender.None;

        private AvatarProperties? currentAvatarProperties; // Hold the properties of the current avatar

        private void Start()
        {

            // Set the listener for the save button click
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveButtonClick);  // Assign listener for saving the avatar
            }

            assetSelectionElementUis.ForEach(element =>
                element.AssetSelectionElement.OnAssetSelected.AddListener(OnAssetSelection));

            // Load saved avatars at the start (handled by SavedAvatarElement.cs)
            SavedAvatarElement.Instance.LoadSavedAvatars();
        }

        public void LoadAvatarCreatorElements()
        {
            mainPanelManager.ShowPanel(panelTemplateSelection);

            avatarManager = new AvatarManager(avatarConfig);

            templateSelectionElement.OnAssetSelected.AddListener(assetData =>
                TemplateSelected((AvatarTemplateData)assetData));
            templateSelectionElement.LoadAndCreateButtons();
        }

        private void SetGender(OutfitGender gender)
        {
            if (gender == this.gender)
            {
                return;
            }

            this.gender = gender;
            assetSelectionElementUis.ForEach(elementUI => elementUI.Gender = gender);
        }

        public void LoadCachedAvatar(GameObject cachedAvatar)
        {
            var loadedAvatar = Instantiate(cachedAvatar);
            var avatarData = loadedAvatar.GetComponent<AvatarData>();
            TransferAvatarData(avatarData.AvatarId, avatarData.AvatarMetadata);
            UpdateAvatar(loadedAvatar);
        }

        public async void OnAssetSelection(IAssetData assetData)
        {
            loading.SetActive(true);
            var updatedAvatar = await avatarManager.UpdateAsset(assetData.AssetType, assetData.Id);
            UpdateAvatar(updatedAvatar);
            loading.SetActive(false);
        }

        private async void TemplateSelected(AvatarTemplateData assetData)
        {
            loading.SetActive(true);
            var avatarProperties = await GetAvatar(assetData);

            SetGender(avatarProperties.Gender);
            onTemplateSelected?.Invoke(avatarProperties);

            // Update the current avatar properties whenever the avatar is customized or changed
            UpdateCurrentAvatar(avatarProperties);

            mainPanelManager.ShowPanel(panelElements);
            loading.SetActive(false);
        }

        private async Task<AvatarProperties> GetAvatar(AvatarTemplateData avatarTemplate)
        {
            var templateAvatarProps = await avatarManager.CreateAvatarFromTemplateAsync(avatarTemplate.Id);
            var avatarProperties = templateAvatarProps.Properties;

            TransferAvatarData(avatarProperties.Id, new AvatarMetadata
            {
                OutfitGender = avatarProperties.Gender,
                BodyType = avatarProperties.BodyType
            });

            UpdateAvatar(templateAvatarProps.AvatarObject);
            return avatarProperties;
        }

        private void UpdateAvatar(GameObject newAvatar)
        {
            AvatarMeshHelper.TransferMesh(newAvatar, AvatarComponentReferences.Instance.Vrik.gameObject);
            AvatarComponentReferences.Instance.HeightCalibrator.CalibrateBody();
            Destroy(newAvatar);
        }

        private void TransferAvatarData(string id, AvatarMetadata metadata)
        {
            var avatarData = AvatarComponentReferences.Instance.AvatarData;
            avatarData.AvatarId = id;
            avatarData.AvatarMetadata = metadata;
        }


        // Save the avatar data using PlayerPrefs
        public void SaveAvatar(AvatarProperties avatarProperties)
        {
            string userId = AuthenticationService.Instance.PlayerId;
            string avatarKey = userId + "_Avatar_" + avatarProperties.Id;
            string avatarDataJson = JsonUtility.ToJson(avatarProperties);
            PlayerPrefs.SetString(avatarKey, avatarDataJson);

            // Store the avatar key in a list (this can be comma-separated)
            string savedKeysListKey = userId + "_SavedAvatarKeys";
            string savedKeys = PlayerPrefs.GetString(savedKeysListKey, "");
            savedKeys += string.IsNullOrEmpty(savedKeys) ? avatarKey : "," + avatarKey;
            PlayerPrefs.SetString(savedKeysListKey, savedKeys);

            PlayerPrefs.Save();
            Debug.Log("Avatar saved: " + avatarKey);
        }

        private void OnSaveButtonClick()
        {
            // Save the current avatar's properties when the button is clicked
            if (!currentAvatarProperties.HasValue)
            {
                Debug.LogError("No avatar properties to save.");
            }
            else
            {
                SaveAvatar(currentAvatarProperties.Value);  // Access the value using `.Value`
            }
        }

        // Update the current avatar properties (used when customizing)
        public void UpdateCurrentAvatar(AvatarProperties avatarProperties)
        {
            currentAvatarProperties = avatarProperties;
            Debug.Log("Current avatar properties updated.");
        }
    }
}