using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.AvatarCreator
{
    public class SavedAvatarElement : MonoBehaviour
    {
        [Header("Properties")]
        [SerializeField] private GameObject buttonPrefab; // Prefab for the button
        [SerializeField] private Transform buttonContainer; // Parent container for the buttons
        [SerializeField] private int iconSize = 64; // Icon size for avatar textures

        // A list to hold all the saved avatars
        private List<AvatarProperties> savedAvatars = new List<AvatarProperties>();

        // Singleton instance for easy access
        public static SavedAvatarElement Instance { get; private set; }

        private void Awake()
        {
            // Ensure Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Method to load all saved avatars from PlayerPrefs
        public void LoadSavedAvatars()
        {
            savedAvatars.Clear(); // Clear previous avatars

            string userId = AuthenticationService.Instance.PlayerId;
            string savedKeysListKey = userId + "_SavedAvatarKeys";

            // Get the saved list of avatar keys (keys that are used for saving avatars)
            string savedKeys = PlayerPrefs.GetString(savedKeysListKey, "");

            if (string.IsNullOrEmpty(savedKeys)) return;

            string[] avatarKeys = savedKeys.Split(',');

            // Iterate through all saved avatar keys in PlayerPrefs
            foreach (var key in avatarKeys)
            {
                string avatarDataJson = PlayerPrefs.GetString(key);
                AvatarProperties avatarProperties = JsonUtility.FromJson<AvatarProperties>(avatarDataJson);

                // Add to the saved avatars list
                savedAvatars.Add(avatarProperties);

                // Create a button for each saved avatar
                CreateAvatarButton(avatarProperties);
            }
        }

        // Create a button for the saved avatar and display its icon
        private async void CreateAvatarButton(AvatarProperties avatarProperties)
        {
            var button = Instantiate(buttonPrefab, buttonContainer); // Instantiate the button prefab
            button.SetActive(true); // Ensure the button is active

            var buttonComponent = button.GetComponent<Button>();
            buttonComponent.onClick.AddListener(() => OnAvatarButtonClicked(avatarProperties)); // Set the click listener

            var buttonImage = button.GetComponentInChildren<Image>(); // Get the button's image component

            // Fetch and set the avatar's texture (using Base64Image)
            var texture = await LoadAvatarTexture(avatarProperties);
            if (texture != null)
            {
                buttonImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }

        // Load the avatar's texture using the Base64Image field
        private async Task<Texture2D> LoadAvatarTexture(AvatarProperties avatarProperties)
        {
            if (string.IsNullOrEmpty(avatarProperties.Base64Image))
            {
                return null; // No image to load
            }

            byte[] imageBytes = System.Convert.FromBase64String(avatarProperties.Base64Image);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes); // Load the texture from the base64 data

            return texture;
        }

        // Callback when an avatar button is clicked
        private void OnAvatarButtonClicked(AvatarProperties avatarProperties)
        {
            Debug.Log("Avatar button clicked: " + avatarProperties.Id);
            // Add functionality to load or display the selected avatar
            // For example, you can update the avatar preview or set the selected avatar as the active avatar
        }
    }
}
