using System;
using System.Threading;
using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using UnityEngine;
using UnityEngine.Events;
using TaskExtensions = ReadyPlayerMe.AvatarCreator.TaskExtensions;

namespace ReadyPlayerMe.Samples.AvatarCreatorElements
{
    public class SessionHandler : MonoBehaviour
    {
        private readonly string sessionStoreKey = "StoredSession";  // Key to store the session in PlayerPrefs

        public UnityEvent<UserSession> OnLogin;

        private async void Start()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            await TaskExtensions.HandleCancellation(Login(cancellationTokenSource.Token));

            if (System.Object.ReferenceEquals(AuthManager.UserSession, null))
            {
                Debug.LogError("Session loading failed.");
            }
        }

        private async Task Login(CancellationToken token)
        {
            if (PlayerPrefs.HasKey(sessionStoreKey))
            {
                // Load session data from PlayerPrefs
                UserSession session = JsonUtility.FromJson<UserSession>(PlayerPrefs.GetString(sessionStoreKey));

                // Check if the session token is expired
                if (IsSessionExpired())
                {
                    Debug.LogWarning("Session expired. Logging in again.");
                    await AuthManager.LoginAsAnonymous(token);  // Re-authenticate if expired
                }
                else
                {
                    AuthManager.SetUser(session);  // Use the loaded session if not expired
                }
            }
            else
            {
                await AuthManager.LoginAsAnonymous(token);  // Perform anonymous login if no session exists
            }

            OnLogin?.Invoke(AuthManager.UserSession);  // Trigger the OnLogin event
        }

        // Store the session data and expiration date in PlayerPrefs
        private void SaveSession()
        {
            PlayerPrefs.SetString(sessionStoreKey, JsonUtility.ToJson(AuthManager.UserSession));

            // Store the expiration time for the session (example: 1 hour from now)
            DateTime expirationTime = DateTime.UtcNow.AddHours(1);  // Set expiration time (customize this)
            PlayerPrefs.SetString("SessionExpiration", expirationTime.ToString("o"));
            PlayerPrefs.Save();  // Save the PlayerPrefs immediately
        }

        // Check if the session is expired
        private bool IsSessionExpired()
        {
            if (PlayerPrefs.HasKey("SessionExpiration"))
            {
                DateTime expirationDate = DateTime.Parse(PlayerPrefs.GetString("SessionExpiration"));
                return DateTime.UtcNow > expirationDate;  // Return true if the session has expired
            }
            return true;  // If no expiration data, assume the session has expired
        }

        // Handle session save on application quit
        private void OnApplicationQuit()
        {
            SaveSession();  // Save session data before the application quits
        }
    }
}
