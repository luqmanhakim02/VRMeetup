using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using Unity.Services.Authentication;
using UnityEngine;

namespace ReadyPlayerMe.XR
{
    public class UserManager : MonoBehaviour
    {
        private const string STORED_SESSION_KEY = "RPM_UserSession";

        private async void Start()
        {
            // Step 1: Wait for Unity Authentication to initialize
            await WaitForUnityAuthentication();

            // Step 2: After the user is signed in, perform login
            await LoginUser();
        }

        private void OnApplicationQuit()
        {
            //PlayerPrefs.SetString(STORED_SESSION_KEY, JsonUtility.ToJson(AuthManager.UserSession));
        }

        private async Task WaitForUnityAuthentication()
        {
            // Ensure Unity Services are initialized before proceeding
            while (!AuthenticationService.Instance.IsSignedIn)
            {
                // Wait for the authentication to be complete
                await Task.Yield();
            }

            Debug.Log("User is authenticated with Unity Services.");
        }

        private async Task LoginUser()
        {
            string userId = AuthenticationService.Instance.PlayerId;

            if (PlayerPrefs.HasKey(STORED_SESSION_KEY + "_" + userId))
            {
                Debug.Log("Load User from PlayerPrefs");
                AuthManager.SetUser(JsonUtility.FromJson<UserSession>(PlayerPrefs.GetString(STORED_SESSION_KEY + "_" + userId)));
            }
            else
            {
                Debug.Log("Create new User");
                await AuthManager.LoginAsAnonymous();
            }
        }
    }
}