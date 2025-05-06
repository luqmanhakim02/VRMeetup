using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR

// Unity 6 Only
#if HAS_MPPM
using Unity.Multiplayer.Playmode;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

#if HAS_PARRELSYNC
using ParrelSync;
#endif

#endif

namespace XRMultiplayer
{
    public class AuthManager1 : MonoBehaviour
    {
        const string k_DebugPrepend = "<color=#938FFF>[Authentication Manager]</color> ";

        /// <summary>
        /// The argument ID to search for in the command line args.
        /// </summary>
        const string k_playerArgID = "PlayerArg";

        /// <summary>
        /// Determines if the AuthenticationManager should use command line args to determine the player ID when launching a build.
        /// </summary>
        [SerializeField] bool m_UseCommandLineArgs = true;

        [Header("User Authentication")]
        [SerializeField] bool m_UseUserAuthentication = false;
        [SerializeField] GameObject m_LoginPanel;
        [SerializeField] TMP_InputField m_UsernameInput;
        [SerializeField] TMP_InputField m_PasswordInput;
        [SerializeField] Button m_LoginButton;
        [SerializeField] Button m_RegisterButton;
        [SerializeField] TextMeshProUGUI m_StatusText;

        private bool m_AuthenticationComplete = false;

        private void Start()
        {
            if (m_UseUserAuthentication && m_LoginPanel != null)
            {
                m_LoginPanel.SetActive(true);

                // Setup button listeners
                if (m_LoginButton != null)
                    m_LoginButton.onClick.AddListener(OnLoginClicked);

                if (m_RegisterButton != null)
                    m_RegisterButton.onClick.AddListener(OnRegisterClicked);
            }
        }

        private async void OnLoginClicked()
        {
            if (m_StatusText != null)
                m_StatusText.text = "Logging in...";

            try
            {
                await InitializeServices();
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(
                    m_UsernameInput.text, m_PasswordInput.text);

                OnAuthenticationComplete();
            }
            catch (Exception e)
            {
                if (m_StatusText != null)
                    m_StatusText.text = $"Login failed: {e.Message}";

                Utils.Log($"{k_DebugPrepend}Login failed: {e.Message}");
            }
        }

        private async void OnRegisterClicked()
        {
            if (m_StatusText != null)
                m_StatusText.text = "Registering...";

            try
            {
                await InitializeServices();
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(
                    m_UsernameInput.text, m_PasswordInput.text);
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(
                    m_UsernameInput.text, m_PasswordInput.text);

                OnAuthenticationComplete();
            }
            catch (Exception e)
            {
                if (m_StatusText != null)
                    m_StatusText.text = $"Registration failed: {e.Message}";

                Utils.Log($"{k_DebugPrepend}Registration failed: {e.Message}");
            }
        }

        private void OnAuthenticationComplete()
        {
            if (m_StatusText != null)
                m_StatusText.text = "Authentication successful!";

            m_AuthenticationComplete = true;

            // Store username for display on the avatar
            PlayerPrefs.SetString("Username", m_UsernameInput.text);
            PlayerPrefs.Save();

            // Cache PlayerId for the network manager
            XRINetworkGameManager.AuthenicationId = AuthenticationService.Instance.PlayerId;

            // Hide login panel
            if (m_LoginPanel != null)
                m_LoginPanel.SetActive(false);
        }

        private async Task InitializeServices()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();
                string playerId = "Player";

#if UNITY_EDITOR
                playerId = "Editor";

#if HAS_MPPM
                playerId += CheckMPPM();
#elif HAS_PARRELSYNC
                playerId += CheckParrelSync();
#endif
#endif

                if (!Application.isEditor && m_UseCommandLineArgs)
                {
                    playerId += GetPlayerIDArg();
                }

                options.SetProfile(playerId);
                Utils.Log($"{k_DebugPrepend}Initializing services with profile {playerId}");

                await UnityServices.InitializeAsync(options);
            }
        }

        /// <summary>
        /// Main authentication function that can either use anonymous or username/password auth
        /// </summary>
        public virtual async Task<bool> Authenticate()
        {
            // If user authentication is enabled, wait for the UI flow to complete
            if (m_UseUserAuthentication)
            {
                // Wait until the user has completed the login/register flow
                while (!m_AuthenticationComplete)
                {
                    await Task.Delay(100);
                }

                return IsAuthenticated();
            }

            // Otherwise, use the existing anonymous authentication
            await InitializeServices();

            if (!AuthenticationService.Instance.IsAuthorized)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            XRINetworkGameManager.AuthenicationId = AuthenticationService.Instance.PlayerId;
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

        string GetPlayerIDArg()
        {
            // Existing implementation
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

#if UNITY_EDITOR
#if HAS_MPPM
        string CheckMPPM()
        {
            // Existing implementation
            Utils.Log($"{k_DebugPrepend}MPPM Found");
            string mppmString = "";
            if(CurrentPlayer.ReadOnlyTags().Length > 0)
            {
                mppmString += CurrentPlayer.ReadOnlyTags()[0];

                // Force input module to disable mouse and touch input to suppress MPPM startup errors.
                var inputModule = FindFirstObjectByType<XRUIInputModule>();
                inputModule.enableMouseInput = false;
                inputModule.enableTouchInput = false;
            }

            return mppmString;
        }
#endif

#if HAS_PARRELSYNC
        string CheckParrelSync()
        {
            // Existing implementation
            Utils.Log($"{k_DebugPrepend}ParrelSync Found");
            string pSyncString = "";
            if (ClonesManager.IsClone()) pSyncString += ClonesManager.GetArgument();
            return pSyncString;
        }
#endif
#endif
    }
}