using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using TMPro;
using System;
using XRMultiplayer;
using CustomMultiplayer;
using UnityEngine.SceneManagement;
//using UnityEditor.Search;

namespace CustomNetwork
{
    public class AuthManagerNew : MonoBehaviour
    {
        const string k_DebugPrepend = "<color=#938FFF>[Authentication Manager New]</color> ";

        public static event Action<string> OnSignInFailed;
        public static event Action<string> OnRegistrationFailed;
        public static event Action OnSignInSuccess;
        public static event Action OnRegistrationSuccess;

        // Configurable validation settings
        [Header("Authentication Settings")]
        [SerializeField] private int m_MinUsernameLength = 3;
        [SerializeField] private int m_MaxUsernameLength = 20;
        [SerializeField] private int m_MinPasswordLength = 8;

        [SerializeField] private GameObject signInDisplay = default;
        [SerializeField] private GameObject lobbyDisplay = default;

        [SerializeField] private CharacterResetter characterResetter;

        public static AuthManagerNew Instance;

        public SignInUI lobbyMenu;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool IsUserSignedIn { get; private set; }

        // Start is called before the first frame update
        async void Start()
        {
            await UnityServices.InitializeAsync();
            bool isSignedIn = AuthenticationService.Instance.IsSignedIn;
            if (isSignedIn)
            {
                lobbyMenu.HandleSignInSuccess();
            }
        }

        private bool ValidateSignInInput(string username, string password)
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(username))
            {
                OnSignInFailed?.Invoke("Username cannot be empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                OnSignInFailed?.Invoke("Password cannot be empty");
                return false;
            }

            // Username length check
            if (username.Length < m_MinUsernameLength || username.Length > m_MaxUsernameLength)
            {
                OnSignInFailed?.Invoke($"Username must be between {m_MinUsernameLength} and {m_MaxUsernameLength} characters");
                return false;
            }

            return true;
        }

        private bool ValidateRegistrationInput(string username, string password)
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(username))
            {
                OnRegistrationFailed?.Invoke("Username cannot be empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                OnRegistrationFailed?.Invoke("Password cannot be empty");
                return false;
            }

            // Username length check
            if (username.Length < m_MinUsernameLength || username.Length > m_MaxUsernameLength)
            {
                OnRegistrationFailed?.Invoke($"Username must be between {m_MinUsernameLength} and {m_MaxUsernameLength} characters");
                return false;
            }

            // Password length check
            if (password.Length < m_MinPasswordLength)
            {
                OnRegistrationFailed?.Invoke($"Password must be at least {m_MinPasswordLength} characters long");
                return false;
            }

            return true;
        }

        private async Task<bool> CreateUserAccount(string username, string password)
        {
            try
            {
                // Attempt to create the account
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

                Utils.Log($"{k_DebugPrepend}User {username} created successfully in Unity Authentication");
                return true;
            }
            catch (AuthenticationException ex)
            {
                // Handle specific authentication exceptions
                string errorMessage = "Registration failed: " + GetAuthenticationErrorMessage(ex);
                OnRegistrationFailed?.Invoke(errorMessage);
                Utils.Log($"{k_DebugPrepend}Authentication error: {ex.Message}");
                return false;
            }
            catch (RequestFailedException ex)
            {
                // Handle request-specific exceptions
                string errorMessage = "Registration failed: " + GetRequestErrorMessage(ex);
                OnRegistrationFailed?.Invoke(errorMessage);
                Utils.Log($"{k_DebugPrepend}Request error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                OnRegistrationFailed?.Invoke($"Unexpected error: {ex.Message}");
                Utils.Log($"{k_DebugPrepend}Unexpected error: {ex.Message}");
                return false;
            }
        }

        public virtual async Task<bool> RegisterUser(string username, string password)
        {
            try
            {
                // Validate input
                if (!ValidateRegistrationInput(username, password))
                {
                    return false;
                }

                bool isRegistrationSuccessful = await CreateUserAccount(username, password);

                if (isRegistrationSuccessful)
                {
                    // Invoke registration success event
                    OnRegistrationSuccess?.Invoke();

                    Utils.Log($"{k_DebugPrepend}User {username} registered successfully");
                    return true;
                }
                else
                {
                    OnRegistrationFailed?.Invoke("Registration failed");
                    Utils.Log($"{k_DebugPrepend}Registration failed for user {username}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnRegistrationFailed?.Invoke(ex.Message);
                Utils.Log($"{k_DebugPrepend}Registration error: {ex.Message}");
                return false;
            }

        }

        private async Task<bool> AuthenticateUser(string username, string password)
        {
            try
            {
                AuthenticationService.Instance.SignOut(true);

                // Attempt to sign in with username and password
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

                Utils.Log($"{k_DebugPrepend}User {username} authenticated successfully");

                XRINetworkGameManager.AuthenicationId = AuthenticationService.Instance.PlayerId;
                XRINetworkGameManager.LocalPlayerName.Value = username;
                return UnityServices.State == ServicesInitializationState.Initialized;
                //return true;
            }
            catch (AuthenticationException ex)
            {
                // Handle specific authentication exceptions
                string errorMessage = "Sign in failed: " + GetAuthenticationErrorMessage(ex);
                OnSignInFailed?.Invoke(errorMessage);
                Utils.Log($"{k_DebugPrepend}Authentication error: {ex.Message}");
                return false;
            }
            catch (RequestFailedException ex)
            {
                // Handle request-specific exceptions
                string errorMessage = "Sign in failed: " + GetRequestErrorMessage(ex);
                OnSignInFailed?.Invoke(errorMessage);
                Utils.Log($"{k_DebugPrepend}Request error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                OnSignInFailed?.Invoke($"Unexpected error: {ex.Message}");
                Utils.Log($"{k_DebugPrepend}Unexpected error: {ex.Message}");
                return false;
            }
        }

        public virtual async Task<bool> SignInWithUsernamePassword(string username, string password)
        {
            try
            {
                // Validate input
                if (!ValidateSignInInput(username, password))
                {
                    return false;
                }

                // Perform authentication
                bool isValidUser = await AuthenticateUser(username, password);

                if (isValidUser)
                {
                    // Store authenticated username
                    XRINetworkGameManager.LocalPlayerName.Value = username;
                    XRINetworkGameManager.AuthenicationId = AuthenticationService.Instance.PlayerId;

                    // Invoke success event
                    OnSignInSuccess?.Invoke();

                    Utils.Log($"{k_DebugPrepend}User {username} signed in successfully with ID: {XRINetworkGameManager.AuthenicationId}");
                    return true;
                }
                else
                {
                    OnSignInFailed?.Invoke("Invalid username or password");
                    Utils.Log($"{k_DebugPrepend}Sign in failed for user {username}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnSignInFailed?.Invoke(ex.Message);
                Utils.Log($"{k_DebugPrepend}Sign in error: {ex.Message}");
                return false;
            }
        }

        private string GetAuthenticationErrorMessage(AuthenticationException ex)
        {
            switch (ex.ErrorCode)
            {
                case 10002: // AuthenticationErrorCodes.InvalidParameters
                    return "Invalid username or password";
                case 10009: // AuthenticationErrorCodes.BannedUser
                    return "User is banned";
                case 10000: // AuthenticationErrorCodes.ClientInvalidUserState
                    return "Invalid user state";
                case 10001: // AuthenticationErrorCodes.ClientNoActiveSession
                    return "No active session";
                case 10007: // AuthenticationErrorCodes.InvalidSessionToken
                    return "Invalid session token";
                case 10010: // AuthenticationErrorCodes.EnvironmentMismatch
                    return "Environment configuration error";
                default:
                    return ex.Message;
            }
        }

        private string GetRequestErrorMessage(RequestFailedException ex)
        {
            return ex.ErrorCode switch
            {
                // Add specific error code translations if needed
                _ => ex.Message
            };
        }

        public virtual bool SignOut()
        {
            try
            {
                AuthenticationService.Instance.SignOut(true);

                IsUserSignedIn = false;
                Utils.Log($"{k_DebugPrepend}User sign out successfully");

                lobbyDisplay.SetActive(false);
                signInDisplay.SetActive(true);

                characterResetter.SetPlayerToOfflinePosition();

                // Restart the scene to reset everything
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

                return true;
            }
            catch (AuthenticationException ex)
            {
                // Handle specific authentication exceptions
                string errorMessage = "Sign in failed: " + GetAuthenticationErrorMessage(ex);
                OnSignInFailed?.Invoke(errorMessage);
                Utils.Log($"{k_DebugPrepend}Sign Out error: {ex.Message}");
                return false;
            }
            catch (RequestFailedException ex)
            {
                // Handle request-specific exceptions
                string errorMessage = "Sign Out failed: " + GetRequestErrorMessage(ex);
                OnSignInFailed?.Invoke(errorMessage);
                Utils.Log($"{k_DebugPrepend}Request error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                OnSignInFailed?.Invoke($"Unexpected error: {ex.Message}");
                Utils.Log($"{k_DebugPrepend}Unexpected error: {ex.Message}");
                return false;
            }
        }

        public void SignOutFromButton()
        {
            Debug.Log("SignOutFromButton clicked!");
            SignOut();
        }

        public virtual async Task<bool> Authenticate()
        {
            await InitializeUnityServices();
            bool isUserSignedIn = IsAuthenticated();
            if (isUserSignedIn)
            {
                XRINetworkGameManager.AuthenicationId = AuthenticationService.Instance.PlayerId;
            }
            return UnityServices.State == ServicesInitializationState.Initialized;
        }

        public static bool IsAuthenticated()
        {
            try
            {
                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (System.Exception e)
            {
                Utils.Log($"{k_DebugPrepend}Checking for AuthenticationService.Instance before initialized.{e}");
                return false;
            }
        }

        private async Task InitializeUnityServices()
        {
            // Check if UGS has not been initialized yet, and initialize.
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                //var options = new InitializationOptions();
                //string playerId = AuthenticationService.Instance.PlayerId;
                ////string playerId = DeterminePlayerId();

                //options.SetProfile(playerId);
                //Utils.Log($"{k_DebugPrepend}Initializing with profile {playerId}");

                //// Initialize UGS using any options defined
                //await UnityServices.InitializeAsync(options);

                await UnityServices.InitializeAsync();
                Utils.Log($"{k_DebugPrepend}Unity Services Initialized");
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }  
}