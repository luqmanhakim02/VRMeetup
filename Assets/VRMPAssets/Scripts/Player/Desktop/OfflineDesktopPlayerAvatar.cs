using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.EventSystems;
using CustomMultiplayer;
using XRMultiplayer;

public class OfflineDesktopPlayerAvatar : MonoBehaviour
{

    public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

    /// <summary>
    /// Gets or sets a value indicating whether the player is muted.
    /// </summary>
    public static bool muted
    {
        get => s_Muted;
        set
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                s_Muted = value;
        }
    }

    /// <summary>
    /// A value indicating whether the player is muted.
    /// </summary>
    static bool s_Muted;

    /// <summary>
    /// The head transform.
    /// </summary>
    [SerializeField] Transform m_HeadTransform;

    /// <summary>
    /// The head renderer.
    /// </summary>
    [SerializeField] SkinnedMeshRenderer m_HeadRend;

    /// <summary>
    /// The voice amplitude curve.
    /// </summary>
    [SerializeField] AnimationCurve m_VoiceCurve;

    [SerializeField] Camera m_PlayerCamera;  // Main camera for first-person view

    // Character Controller settings
    private CharacterController characterController;
    //[SerializeField] float moveSpeed = 5.0f;
    [SerializeField] float lookSensitivity = 2.0f;

    // Speed control
    [SerializeField] float moveSpeed = 5f;  // Adjust speed as needed
    [SerializeField] float smoothTime = 0.1f;  // Adjust smoothness as needed

    // Velocity smoothing
    private Vector3 currentVelocity = Vector3.zero;  // The current velocity of the player

    // Camera control variables
    private float verticalLookRotation = 0f;
    private float horizontalInput;
    private float verticalInput;

    // Voice chat settings

    /// <summary>
    /// The mouth blend smoothing.
    /// </summary>
    [SerializeField] float m_MouthBlendSmoothing = 5.0f;

    /// <summary>
    /// The microphone loudness.
    /// </summary>
    float m_MicLoudness;

    /// <summary>
    /// The microphone device name.
    /// </summary>
    string m_Device;

    /// <summary>
    /// The sample window.
    /// </summary>
    int m_SampleWindow = 128;

    /// <summary>
    /// The clip record.
    /// </summary>
    AudioClip m_ClipRecord;

    /// <summary>
    /// The voice destination volume.
    /// </summary>
    float m_VoiceDestinationVolume;

    bool m_MicInitialized = false;

    // UI Interaction Mode variables
    private bool isUIInteractionMode = false;  // Track UI interaction state

    void Start()
    {
        characterController = GetComponent<CharacterController>();  // Get the CharacterController component

        // Lock the cursor for FPS controls initially
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        NetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
        VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
        NetworkGameManager.Connected.Subscribe(connected =>
        {
            gameObject.SetActive(!connected);
        });
    }

    void OnDisable()
    {
        NetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
        VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
        StopMicrophone();
        NetworkGameManager.Connected.Unsubscribe(connected =>
        {
            gameObject.SetActive(!connected);
        });
    }

    void Update()
    {
        // Toggle UI interaction mode
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleUIInteractionMode();
        }

        if (!isUIInteractionMode)
        {
            HandleMovement();
            HandleMouseLook();
        }

        HandleVoiceChat();
    }

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
            characterController.enabled = false;  // Disable movement script

            // Allow cursor to interact with UI (reset selected object)
            EventSystem.current.SetSelectedGameObject(null);  // Optional: reset selected object in UI
        }
        else
        {
            // Return to FPS mode
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;  // Hide the cursor again

            // Enable player movement and camera control again
            characterController.enabled = true;  // Enable FPS movement script
        }
    }

    void HandleMovement()
    {
        // Get the input from the keyboard (WASD or Arrow Keys)
        horizontalInput = Input.GetAxis("Horizontal");  // A/D or Left/Right Arrow keys
        verticalInput = Input.GetAxis("Vertical");  // W/S or Up/Down Arrow keys

        // Create movement vector
        //Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        //moveDirection = moveDirection.normalized * moveSpeed * Time.deltaTime;  // Normalize for consistent movement speed

        //// Move the character controller
        //characterController.Move(moveDirection);

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
    }

    void HandleMouseLook()
    {
        // Get mouse movement for looking around
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        // Rotate the whole player (left and right)
        transform.Rotate(Vector3.up, mouseX);

        // Rotate the camera (up and down)
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -80f, 80f);
        m_HeadTransform.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);

        // Make the camera follow the head position
        m_PlayerCamera.transform.position = m_HeadTransform.position;
        m_PlayerCamera.transform.rotation = m_HeadTransform.rotation;
    }

    void HandleVoiceChat()
    {
        if (!s_Muted)
        {
            m_MicLoudness = LevelMax();

            m_VoiceDestinationVolume = Mathf.Clamp01(Mathf.Lerp(m_VoiceDestinationVolume, m_MicLoudness, Time.deltaTime * m_MouthBlendSmoothing));

            float appliedCurve = m_VoiceCurve.Evaluate(m_VoiceDestinationVolume);
            voiceAmp.Value = appliedCurve;
            m_HeadRend.SetBlendShapeWeight(0, 100 - appliedCurve * 100);
        }
        else
        {
            voiceAmp.Value = 0.0f;
        }
    }

    void MicrophonePermissionGranted(bool granted)
    {
        if (granted)
        {
            InitMic();
        }
    }
    void UpdatePlayerColor(Color color)
    {
        m_HeadRend.materials[2].color = color;
    }

    /// <summary>
    /// Initializes the microphone, called from <see cref="VoiceChatManager.s_HasMicrophonePermission" callback/>.
    /// </summary>
    void InitMic()
    {
        m_MicInitialized = true;
        m_Device ??= Microphone.devices[0];
        m_ClipRecord = Microphone.Start(m_Device, true, 999, 44100);
    }

    /// <summary>
    /// Stops the microphone.
    /// </summary>
    void StopMicrophone()
    {
        m_MicInitialized = false;
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Microphone.End(m_Device);
        }
        else
        {
            muted = true;
        }
    }

    /// <summary>
    /// Gets the maximum level of the microphone input.
    /// </summary>
    /// <returns>The maximum level of the microphone input.</returns>
    float LevelMax()
    {
        if (!m_MicInitialized) return 0;
        float levelMax = 0;
        float[] waveData = new float[m_SampleWindow];
        int micPosition = Microphone.GetPosition(null) - (m_SampleWindow + 1);
        if (micPosition < 0) return 0;
        m_ClipRecord.GetData(waveData, micPosition);
        for (int i = 0; i < m_SampleWindow; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        return levelMax;
    }
}
