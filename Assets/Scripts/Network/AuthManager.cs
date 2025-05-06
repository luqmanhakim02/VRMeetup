using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace XRMultiplayer
{
    public class AuthManager : MonoBehaviour
    {
        const string k_DebugPrepend = "<color=#938FFF>[Authentication Manager]</color> ";
        const string k_playerArgID = "PlayerArg";

        // Custom username authentication events
        public static event Action<string> OnSignInFailed;
        public static event Action<string> OnRegistrationFailed;
        public static event Action OnSignInSuccess;
        public static event Action OnRegistrationSuccess;

        // Configurable validation settings
        [Header("Authentication Settings")]
        [SerializeField] private int m_MinUsernameLength = 3;
        [SerializeField] private int m_MaxUsernameLength = 20;
        [SerializeField] private int m_MinPasswordLength = 8;

        // Configurable authentication settings
        [SerializeField] private bool m_UseCommandLineArgs = true;

        /// <summary>
        /// Custom Authentication function for username and password
        /// </summary>
        public virtual async Task<bool> SignInWithUsernamePassword(string username, string password)
        {
            try
            {
                // Validate input
                if (!ValidateSignInInput(username, password))
                {
                    return false;
                }

                // Initialize services if not already initialized
                await InitializeUnityServices();

                // Perform authentication
                bool isValidUser = await AuthenticateUser(username, password);

                if (isValidUser)
                {
                    // Store authenticated username
                    XRINetworkGameManager.LocalPlayerName.Value = username;
                    XRINetworkGameManager.AuthenicationId = AuthenticationService.Instance.PlayerId;

                    // Invoke success event
                    OnSignInSuccess?.Invoke();

                    Utils.Log($"{k_DebugPrepend}User {username} signed in successfully");
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

        /// <summary>
        /// Custom Registration function for username and password
        /// </summary>
        public virtual async Task<bool> RegisterUser(string username, string password)
        {
            try
            {
                // Validate input
                if (!ValidateRegistrationInput(username, password))
                {
                    return false;
                }

                // Initialize services if not already initialized
                await InitializeUnityServices();

                // Perform user registration
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

        /// <summary>
        /// Validate sign-in input
        /// </summary>
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

        /// <summary>
        /// Validate registration input
        /// </summary>
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

        /// <summary>
        /// Authenticate user using Unity Authentication
        /// </summary>
        private async Task<bool> AuthenticateUser(string username, string password)
        {
            try
            {
                // Sign out any existing session
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignOut();
                }

                // Attempt to sign in with username and password
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

                Utils.Log($"{k_DebugPrepend}User {username} authenticated successfully");
                return true;
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

        /// <summary>
        /// Create user account using Unity Authentication
        /// </summary>
        private async Task<bool> CreateUserAccount(string username, string password)
        {
            try
            {
                // Sign out any existing session
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignOut();
                }

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

        /// <summary>
        /// Helper method to translate authentication exceptions
        /// </summary>
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

        /// <summary>
        /// Helper method to translate request exceptions
        /// </summary>
        private string GetRequestErrorMessage(RequestFailedException ex)
        {
            return ex.ErrorCode switch
            {
                // Add specific error code translations if needed
                _ => ex.Message
            };
        }

        /// <summary>
        /// Initialize Unity Services with appropriate profile
        /// </summary>
        private async Task InitializeUnityServices()
        {
            // Check if UGS has not been initialized yet, and initialize.
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();
                string playerId = "Player";
                //string playerId = DeterminePlayerId();

                options.SetProfile(playerId);
                Utils.Log($"{k_DebugPrepend}Initializing with profile {playerId}");

                // Initialize UGS using any options defined
                await UnityServices.InitializeAsync(options);
            }
        }

        /// <summary>
        /// Determine the player ID based on environment
        /// </summary>
//        private string DeterminePlayerId()
//        {
//            string playerId = "Player";

//#if UNITY_EDITOR
//            playerId = "Editor";

//#if HAS_MPPM
//            //Check for MPPM
//            playerId += CheckMPPM();
//#elif HAS_PARRELSYNC
//            // Check for ParrelSync
//            playerId += CheckParrelSync();
//#endif
//#endif
//            // Check for command line args in builds
//            if (!Application.isEditor && m_UseCommandLineArgs)
//            {
//                playerId += GetPlayerIDArg();
//            }

//            return playerId;
//        }

        /// <summary>
        /// Fallback authentication method
        /// </summary>
        public virtual async Task<bool> Authenticate()
        {
            await InitializeUnityServices();
            return UnityServices.State == ServicesInitializationState.Initialized;
        }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        public static bool IsAuthenticated()
        {
            try
            {
                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (Exception e)
            {
                Utils.Log($"{k_DebugPrepend}Checking for AuthenticationService.Instance before initialized.{e}");
                return false;
            }
        }

        /// <summary>
        /// Get player ID from command line arguments
        /// </summary>
        private string GetPlayerIDArg()
        {
            string playerID = "";
            string[] args = System.Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                arg.ToLower();
                if (arg.ToLower().Contains(k_playerArgID.ToLower()))
                {
                    var splitArgs = arg.Split(':');
                    if (splitArgs.Length > 0)
                    {
                        playerID += splitArgs[1];
                    }
                }
            }
            return playerID;
        }
    }
}