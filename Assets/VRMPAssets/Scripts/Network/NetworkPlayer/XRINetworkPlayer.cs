using UnityEngine;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using Unity.Collections;
using System;
using Unity.Services.Vivox;
using Unity.XR.CoreUtils.Bindings.Variables;
using CustomMultiplayer;

namespace XRMultiplayer
{
    /// <summary>
    /// XRINetworkPlayer class used for simple interactions.
    /// </summary>
    public class XRINetworkPlayer : NetworkBehaviour
    {
        /// <summary>
        /// Speed at which voice amplitude changes.
        /// </summary>
        const float k_VoiceAmplitudeSpeed = 15.0f;

        /// <summary>
        /// Singleton Reference for the Local Player.
        /// </summary>
        public static XRINetworkPlayer LocalPlayer;

        [Header("Avatar Transform References"), Tooltip("Assign to local avatar transform.")]
        /// <summary>
        /// Non-Local player transforms.
        /// </summary>
        public Transform head;

        /// <summary>
        /// Non-Local player transforms.
        /// </summary>
        public Transform leftHand;

        /// <summary>
        /// Non-Local player transforms.
        /// </summary>
        public Transform rightHand;

        /// <summary>
        /// Action called when the player name is updated.
        /// </summary>
        public Action<string> onNameUpdated;

        /// <summary>
        /// Action called when the player color is updated.
        /// </summary>
        public Action<Color> onColorUpdated;

        /// <summary>
        /// Action called when the Local Player is finished spawning in.
        /// </summary>
        public Action onSpawnedLocal;

        /// <summary>
        /// Action called when the Local Player is finished spawning in.
        /// </summary>
        public Action onSpawnedAll;

        /// <summary>
        /// Action called when the player color is updated.
        /// </summary>
        public Action<XRINetworkPlayer> onDisconnected;

        /// <summary>
        /// Bindable Variable used for other clients to mute this user locally.
        /// </summary>
        public BindableVariable<bool> squelched = new BindableVariable<bool>(false);

        /// <summary>
        /// Current Voice Amplitude driven from Vivox.
        /// </summary>
        public float playerVoiceAmp { get => m_VoiceAmplitudeCurrent; }
        float m_VoiceAmplitudeCurrent;

        /// <summary>
        /// Player Voice Id string that reads from the internal NetworkVariable for the Player Voice Id.
        /// </summary>
        public string playerVoiceId { get => m_PlayerVoiceId.Value.ToString(); }
        readonly NetworkVariable<FixedString128Bytes> m_PlayerVoiceId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Player Name string that reads from the internal NetworkVariable for the Player Name.
        /// </summary>
        public string playerName { get => m_PlayerName.Value.ToString(); }
        readonly NetworkVariable<FixedString128Bytes> m_PlayerName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Player Color that reads from the internal NetworkVariable for the Player Color.
        /// </summary>
        public Color playerColor { get => m_PlayerColor.Value; }
        readonly NetworkVariable<Color> m_PlayerColor = new(Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [HideInInspector] public readonly NetworkVariable<bool> selfMuted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>
        /// Player Name Tag.
        /// </summary>
        [Header("Player Name Tag"), SerializeField, Tooltip("Player Name Tag.")] protected bool m_UpdateObjectName = true;

        /// <summary>
        /// Hand Objects to be disabled for the local player.
        /// </summary>
        [Header("Networked Hands"), SerializeField, Tooltip("Hand Objects to be disabled for the local player.")] protected GameObject[] m_handsObjects;

        /// <summary>
        /// Player Name Tag.
        /// </summary>
        [Header("Player Name Tag"), SerializeField, Tooltip("Player Name Tag.")] protected PlayerNameTag m_PlayerNameTag;

        /// <summary>
        /// Internal references to the Local Player Transforms.
        /// </summary>
        protected Transform m_LeftHandOrigin, m_RightHandOrigin, m_HeadOrigin;

        /// <summary>
        /// Reference to the local player XR Origin
        /// </summary>
        protected XROrigin m_XROrigin;

        /// <summary>
        /// If the player has been connected to the game.
        /// </summary>
        protected bool m_InitialConnected = false;

        /// <summary>
        /// Reference to the VoiceChatManager.
        /// </summary>
        protected VoiceChatManager m_VoiceChat;

        /// <summary>
        /// Reference to the VivoxParticipant.
        /// </summary>
        protected VivoxParticipant m_VivoxParticipant;

        /// <summary>
        /// Time to update the voice position.
        /// </summary>
        protected float m_VoicePositionUpdateTime = .1f, m_VoiceUpdatePosotionDelta = .05f;

        /// <summary>
        /// Destination for the voice amplitude.
        /// </summary>
        protected float m_VoiceAmplitudeDestination;

        /// <summary>
        /// Timer to check the voice position.
        /// </summary>
        protected float m_VoicePositionCheckTimer;

        /// <summary>
        /// Previous position of the head.
        /// </summary>
        protected Vector3 m_PrevHeadPos;

        // UI Interaction Mode
        private bool isUIInteractionMode = false;  // Track UI interaction state
        private bool isMovementLocked = false; // Track movement state

        private CharacterController characterController;  // Fallback for desktop controls
        private float verticalLookRotation = 0f;

        // Speed control
        private float moveSpeed = 5f;  // Adjust speed as needed
        private float smoothTime = 0.1f;  // Adjust smoothness as needed

        // Velocity smoothing
        private Vector3 currentVelocity = Vector3.zero;  // The current velocity of the player

        protected void Awake()
        {
            m_VoiceChat = FindFirstObjectByType<VoiceChatManager>();
            m_VoicePositionCheckTimer = m_VoicePositionUpdateTime;

            // Check if XR Origin exists, if not fallback to desktop control
            m_XROrigin = FindFirstObjectByType<XROrigin>();
            if (m_XROrigin == null)
            {
                // If XR Origin is not found, use CharacterController for desktop controls
                characterController = GetComponent<CharacterController>();
                if (characterController == null)
                {
                    Debug.LogError("No CharacterController found! Please add one to the player.");
                }
            }
        }

        ///<inheritdoc/>
        protected virtual void OnEnable()
        {
            m_PlayerName.OnValueChanged += UpdatePlayerName;
            m_PlayerColor.OnValueChanged += UpdatePlayerColor;
        }

        ///<inheritdoc/>
        protected virtual void OnDisable()
        {
            m_PlayerName.OnValueChanged -= UpdatePlayerName;
            m_PlayerColor.OnValueChanged -= UpdatePlayerColor;
        }

        ///<inheritdoc/>
        protected virtual void Update()
        {
            if (IsOwner && NetworkGameManager.Instance.positionalVoiceChat)
            {
                if (Time.time > m_VoicePositionCheckTimer)
                {
                    m_VoicePositionCheckTimer += m_VoicePositionUpdateTime;

                    if (Vector3.Distance(m_PrevHeadPos, m_HeadOrigin.position) > m_VoiceUpdatePosotionDelta)
                    {
                        m_PrevHeadPos = m_HeadOrigin.position;
                        if (NetworkGameManager.Instance.positionalVoiceChat)
                        {
                            m_VoiceChat.Set3DAudio(m_HeadOrigin);
                        }
                    }
                }
            }

            m_VoiceAmplitudeCurrent = Mathf.Lerp(m_VoiceAmplitudeCurrent, m_VoiceAmplitudeDestination, Time.deltaTime * k_VoiceAmplitudeSpeed);

            // Toggle UI interaction mode with the 'V' key
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleUIInteractionMode();
            }

            // Use desktop controls if no XR Origin is found
            // Allow movement and look around if not in UI interaction mode
            if (!isUIInteractionMode && m_XROrigin == null)
            {
                HandleMovement();
                HandleMouseLook();
            }
        }

        ///<inheritdoc/>
        protected virtual void LateUpdate()
        {
            if (!IsOwner) return;

            if (m_XROrigin != null)
            {
                // Update position and rotation for the hands
                leftHand.SetPositionAndRotation(m_LeftHandOrigin.position, m_LeftHandOrigin.rotation);
                rightHand.SetPositionAndRotation(m_RightHandOrigin.position, m_RightHandOrigin.rotation);

                // Update the position and rotation for the head
                head.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
            }
        }

        ///<inheritdoc/>
        public override void OnDestroy()
        {
            base.OnDestroy();

            if (IsOwner)
            {
                NetworkGameManager.LocalPlayerName.Unsubscribe(UpdateLocalPlayerName);
                NetworkGameManager.LocalPlayerColor.Unsubscribe(UpdateLocalPlayerColor);
                m_VoiceChat.selfMuted.Unsubscribe(SelfMutedChanged);
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                NetworkGameManager.Instance.PlayerLeft(NetworkObject.OwnerClientId);
            }

            m_PlayerColor.OnValueChanged -= UpdatePlayerColor;
        }

        ///<inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                LocalPlayer = this;
                NetworkGameManager.Instance.LocalPlayerConnected(NetworkObject.OwnerClientId);

                // Set the player position to the specified spawn location
                //transform.position = new Vector3(0f, 1.07999992f, 0.299917549f);
                transform.position = new Vector3(0f, 0.116f, 0f);


                if (m_XROrigin != null)
                {
                    m_HeadOrigin = m_XROrigin.Camera.transform;
                }
                else
                {
                    Debug.Log("No XR Rig Available, falling back to desktop controls.");
                }

                SetupLocalPlayer();
            }
            CompleteSetup();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PlayerHudNotification.Instance.ShowText($"<b>{m_PlayerName.Value}</b> left");
            onDisconnected?.Invoke(this);
        }

        public void SetHandOrigins(Transform left, Transform right)
        {
            m_LeftHandOrigin = left;
            m_RightHandOrigin = right;
        }

        protected virtual void SetupLocalPlayer()
        {
            foreach (var hand in m_handsObjects)
            {  
                hand.SetActive(false);
            }

            m_PlayerName.Value = new FixedString128Bytes(NetworkGameManager.LocalPlayerName.Value);
            m_PlayerColor.Value = NetworkGameManager.LocalPlayerColor.Value;

            Debug.Log("SINIII XRINETWORKPLAYER NAME " + m_PlayerName.Value);

            NetworkGameManager.LocalPlayerName.Subscribe(UpdateLocalPlayerName);
            NetworkGameManager.LocalPlayerColor.Subscribe(UpdateLocalPlayerColor);
            m_VoiceChat.selfMuted.Subscribe(SelfMutedChanged);
            m_VoiceChat.ToggleSelfMute(true, true);

            onSpawnedLocal?.Invoke();
        }

        void SelfMutedChanged(bool muted)
        {
            selfMuted.Value = muted;
        }

        protected virtual void UpdateLocalPlayerColor(Color color)
        {
            m_PlayerColor.Value = NetworkGameManager.LocalPlayerColor.Value;
        }

        protected virtual void UpdateLocalPlayerName(string name)
        {
            m_PlayerName.Value = new FixedString128Bytes(NetworkGameManager.LocalPlayerName.Value);
        }

        void CompleteSetup()
        {
            NetworkGameManager.Instance.PlayerJoined(NetworkObject.OwnerClientId);
            UpdatePlayerName(new FixedString128Bytes(""), m_PlayerName.Value);
            UpdatePlayerColor(Color.white, m_PlayerColor.Value);

            WorldCanvas worldCanvas = FindFirstObjectByType<WorldCanvas>();
            if (worldCanvas != null)
            {
                Canvas localCanvas = m_PlayerNameTag.GetComponentInParent<Canvas>();
                worldCanvas.SetupPlayerNameTag(this, m_PlayerNameTag);
                Destroy(localCanvas.gameObject);
            }
            else
            {
                m_PlayerNameTag.SetupNameTag(this);
            }

            onSpawnedAll?.Invoke();
        }

        void UpdatePlayerName(FixedString128Bytes oldName, FixedString128Bytes currentName)
        {
            onNameUpdated?.Invoke(currentName.ToString());

            if (!m_InitialConnected & !string.IsNullOrEmpty(currentName.ToString()))
            {
                m_InitialConnected = true;
                if (!IsLocalPlayer)
                    PlayerHudNotification.Instance.ShowText($"<b>{playerName}</b> joined");
            }

            if (m_UpdateObjectName)
                gameObject.name = currentName.ToString();
        }

        void UpdatePlayerColor(Color oldColor, Color newColor)
        {
            onColorUpdated?.Invoke(newColor);
        }

        void UpdatePlayerVoiceEnergy(float current)
        {
            m_VoiceAmplitudeDestination = Mathf.Clamp01(current);
        }

        public void SetupVoicePlayer()
        {
            m_VivoxParticipant = m_VoiceChat.GetVivoxParticipantById(playerVoiceId);
            if (m_VivoxParticipant != null)
            {
                m_VivoxParticipant.ParticipantAudioEnergyChanged += ParticipantAudioEnergyChanged;
            }
            else
            {
                Utils.Log($"No Participant with id: {playerVoiceId}", 1);
            }

            if (!VoiceChatManager.m_PlayersDictionary.ContainsKey(playerVoiceId))
            {
                VoiceChatManager.AddNewVivoxPlayer(playerVoiceId, this);
            }
        }

        private void ParticipantAudioEnergyChanged()
        {
            UpdatePlayerVoiceEnergy((float)m_VivoxParticipant.AudioEnergy);
        }

        public void SetVoiceId(string voiceId)
        {
            if (!IsOwner) return;
            m_PlayerVoiceId.Value = new FixedString128Bytes(voiceId);
            SetupVoicePlayer();
            if (NetworkGameManager.Instance.positionalVoiceChat)
            {
                m_VoiceChat.Set3DAudio(m_HeadOrigin);
            }
        }

        public void ToggleSquelch()
        {
            if (m_VivoxParticipant != null)
            {
                squelched.Value = !squelched.Value;
                if (squelched.Value)
                    m_VivoxParticipant.MutePlayerLocally();
                else
                    m_VivoxParticipant.UnmutePlayerLocally();
            }
        }

        // Handle switching between UI interaction and normal mode
        void ToggleUIInteractionMode()
        {
            // Toggle UI interaction mode
            isUIInteractionMode = !isUIInteractionMode;

            if (isUIInteractionMode)
            {
                // Lock camera movement and show the cursor for UI interaction
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Optionally, stop player movement or set up an interaction mode for UI
                isMovementLocked = true;  // Lock movement
                characterController.enabled = false;  // Disable movement script
            }
            else
            {
                // Return to FPS mode
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;  // Hide the cursor again

                // Enable player movement and camera control again
                isMovementLocked = false;
                characterController.enabled = true;  // Enable FPS movement script
            }
        }

        // Desktop movement handling
        private void HandleMovement()
        {
            if (isMovementLocked) return; // Prevent movement when locked

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // Get movement direction based on input
            Vector3 targetMoveDirection = transform.right * horizontalInput + transform.forward * verticalInput;

            // Smoothly adjust the velocity using SmoothDamp (ensuring smooth transitions)
            Vector3 smoothMoveDirection = Vector3.SmoothDamp(
                currentVelocity,       // The current velocity
                targetMoveDirection,   // The target velocity
                ref currentVelocity,   // A reference to store the smoothed result
                smoothTime);           // Time to smooth out (increase this for more smoothing)

            // Apply movement based on smoothed velocity
            if (characterController != null)
            {
                characterController.Move(smoothMoveDirection * moveSpeed * Time.deltaTime);  // Use the smoothed movement
            }

            // Rotate hands along with player rotation (based on movement direction)
            if (m_XROrigin == null)
            {
                leftHand.Rotate(Vector3.up, horizontalInput * 10f);  // Adjust rotation speed as needed
                rightHand.Rotate(Vector3.up, horizontalInput * 10f); // Adjust rotation speed as needed
            }
        }

        // Desktop mouse look handling
        private void HandleMouseLook()
        {
            if (isMovementLocked) return; // Prevent mouse look when locked

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            transform.Rotate(Vector3.up, mouseX);  // Rotate horizontally based on mouse movement

            verticalLookRotation -= mouseY;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -80f, 80f);
            head.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);  // Rotate vertically based on mouse movement

            // Make the camera follow the head position
            Camera.main.transform.position = head.position;
            Camera.main.transform.rotation = head.rotation;
            
        }
    }
}
