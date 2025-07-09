using CustomMultiplayer;
using Unity.Services.Relay.Models;
using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Android;

#region oldScript

//namespace XRMultiplayer
//{
//    /// <summary>
//    /// Represents the offline player avatar.
//    /// </summary>
//    public class OfflinePlayerAvatar : MonoBehaviour
//    {
//        public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

//        /// <summary>
//        /// Gets or sets a value indicating whether the player is muted.
//        /// </summary>
//        public static bool muted
//        {
//            get => s_Muted;
//            set
//            {
//                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
//                    s_Muted = value;
//            }
//        }

//        /// <summary>
//        /// A value indicating whether the player is muted.
//        /// </summary>
//        static bool s_Muted;

//        /// <summary>
//        /// The head transform.
//        /// </summary>
//        [SerializeField] Transform m_HeadTransform;

//        /// <summary>
//        /// The head renderer.
//        /// </summary>
//        [SerializeField] SkinnedMeshRenderer m_HeadRend;

//        /// <summary>
//        /// The voice amplitude curve.
//        /// </summary>
//        [SerializeField] AnimationCurve m_VoiceCurve;

//        /// <summary>
//        /// The head origin.
//        /// </summary>
//        Transform m_HeadOrigin;

//        /// <summary>
//        /// The mouth blend smoothing.
//        /// </summary>
//        [SerializeField] float m_MouthBlendSmoothing = 5.0f;

//        /// <summary>
//        /// The microphone loudness.
//        /// </summary>
//        float m_MicLoudness;

//        /// <summary>
//        /// The microphone device name.
//        /// </summary>
//        string m_Device;

//        /// <summary>
//        /// The sample window.
//        /// </summary>
//        int m_SampleWindow = 128;

//        /// <summary>
//        /// The clip record.
//        /// </summary>
//        AudioClip m_ClipRecord;

//        /// <summary>
//        /// The voice destination volume.
//        /// </summary>
//        float m_VoiceDestinationVolume;

//        bool m_MicInitialized = false;

//        /// <inheritdoc/>
//        void Start()
//        {
//            XROrigin rig = FindFirstObjectByType<XROrigin>();
//            m_HeadOrigin = rig.Camera.transform;

//        }

//        void OnEnable()
//        {
//            NetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
//            VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
//            NetworkGameManager.Connected.Subscribe(connected =>
//            {
//                gameObject.SetActive(!connected);
//            });
//        }

//        void OnDisable()
//        {
//            NetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
//            VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
//            StopMicrophone();
//            NetworkGameManager.Connected.Unsubscribe(connected =>
//            {
//                gameObject.SetActive(!connected);
//            });
//        }

//        /// <inheritdoc/>
//        private void LateUpdate()
//        {
//            m_HeadTransform.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
//        }

//        /// <inheritdoc/>
//        void Update()
//        {
//            if (!s_Muted)
//            {
//                m_MicLoudness = LevelMax();

//                m_VoiceDestinationVolume = Mathf.Clamp01(Mathf.Lerp(m_VoiceDestinationVolume, m_MicLoudness, Time.deltaTime * m_MouthBlendSmoothing));

//                float appliedCurve = m_VoiceCurve.Evaluate(m_VoiceDestinationVolume);
//                voiceAmp.Value = appliedCurve;
//                m_HeadRend.SetBlendShapeWeight(0, 100 - appliedCurve * 100);
//            }
//            else
//            {
//                voiceAmp.Value = 0.0f;
//            }
//        }

//        void MicrophonePermissionGranted(bool granted)
//        {
//            if (granted)
//            {
//                InitMic();
//            }
//        }

//        void UpdatePlayerColor(Color color)
//        {
//            m_HeadRend.materials[2].color = color;
//        }

//        /// <summary>
//        /// Initializes the microphone, called from <see cref="VoiceChatManager.s_HasMicrophonePermission" callback/>.
//        /// </summary>
//        void InitMic()
//        {
//            m_MicInitialized = true;
//            m_Device ??= Microphone.devices[0];
//            m_ClipRecord = Microphone.Start(m_Device, true, 999, 44100);
//        }

//        /// <summary>
//        /// Stops the microphone.
//        /// </summary>
//        void StopMicrophone()
//        {
//            m_MicInitialized = false;
//            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
//            {
//                Microphone.End(m_Device);
//            }
//            else
//            {
//                s_Muted = true;
//            }
//        }

//        /// <summary>
//        /// Gets the maximum level of the microphone input.
//        /// </summary>
//        /// <returns>The maximum level of the microphone input.</returns>
//        float LevelMax()
//        {
//            if (!m_MicInitialized) return 0;
//            float levelMax = 0;
//            float[] waveData = new float[m_SampleWindow];
//            int micPosition = Microphone.GetPosition(null) - (m_SampleWindow + 1); // null means the first microphone
//            if (micPosition < 0) return 0;
//            m_ClipRecord.GetData(waveData, micPosition);
//            // Getting a peak on the last 128 samples
//            for (int i = 0; i < m_SampleWindow; i++)
//            {
//                float wavePeak = waveData[i] * waveData[i];
//                if (levelMax < wavePeak)
//                {
//                    levelMax = wavePeak;
//                }
//            }
//            return levelMax;
//        }
//    }
//}

#endregion


#region new script

namespace XRMultiplayer
{
    /// <summary> Lightweight lobby avatar for mic test & colour preview. </summary>
    public class OfflinePlayerAvatar : MonoBehaviour
    {
        public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

        public static bool muted
        {
            get => s_Muted;
            set
            {
                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    s_Muted = value;
            }
        }
        static bool s_Muted;

        [Header("RPM Mesh Bits")]
        [SerializeField] SkinnedMeshRenderer headRenderer;   // drag the RPM face renderer here
        [SerializeField] int jawBlendShapeIndex = -1;        // set -1 to disable mouth anim

        [Header("Voice Visual")]
        [SerializeField] AnimationCurve voiceCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] float mouthBlendSmoothing = 5f;

        float micLoudness;
        string micDevice;
        readonly int sampleWindow = 128;
        AudioClip clipRecord;
        bool micInitialized;

        /* ---------- Unity life-cycle ---------- */

        void OnEnable()
        {
            NetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
            VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
            NetworkGameManager.Connected.Subscribe(OnNetworkConnectedChanged);
        }
        void OnDisable()
        {
            NetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
            VoiceChatManager.s_HasMicrophonePermission.Unsubscribe(MicrophonePermissionGranted);
            NetworkGameManager.Connected.Unsubscribe(OnNetworkConnectedChanged);
            StopMicrophone();
        }

        void Update()
        {
            if (muted) { voiceAmp.Value = 0; return; }

            micLoudness = LevelMax();
            float dest = Mathf.Clamp01(micLoudness);
            float appliedCurve = voiceCurve.Evaluate(dest);
            voiceAmp.Value = appliedCurve;

            // optional lipsync – only if you typed a valid jaw index in Inspector
            if (jawBlendShapeIndex >= 0 && headRenderer)
            {
                float current = headRenderer.GetBlendShapeWeight(jawBlendShapeIndex);
                float target = 100f - appliedCurve * 100f;
                float smoothed = Mathf.Lerp(current, target, Time.deltaTime * mouthBlendSmoothing);
                headRenderer.SetBlendShapeWeight(jawBlendShapeIndex, smoothed);
            }
        }

        /* ---------- Event handlers ---------- */

        void OnNetworkConnectedChanged(bool connected)
        {
            // show when NOT connected, hide when connected
            gameObject.SetActive(!connected);
        }

        void UpdatePlayerColor(Color colour)
        {
            // RPM usually has all body parts share one material, guard against out-of-range
            foreach (var mat in headRenderer.materials)
                mat.color = colour;
        }

        void MicrophonePermissionGranted(bool granted)
        {
            if (granted) InitMic();
        }

        /* ---------- Mic helpers ---------- */

        void InitMic()
        {
            micInitialized = true;
            micDevice ??= Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
            if (!string.IsNullOrEmpty(micDevice))
                clipRecord = Microphone.Start(micDevice, true, 10, 44100);
        }
        void StopMicrophone()
        {
            micInitialized = false;
            if (!string.IsNullOrEmpty(micDevice)) Microphone.End(micDevice);
        }
        float LevelMax()
        {
            if (!micInitialized || string.IsNullOrEmpty(micDevice)) return 0;
            int micPos = Microphone.GetPosition(micDevice) - (sampleWindow + 1);
            if (micPos < 0) return 0;
            float[] wave = new float[sampleWindow];
            clipRecord.GetData(wave, micPos);

            float max = 0;
            for (int i = 0; i < sampleWindow; i++)
            {
                float peak = wave[i] * wave[i];
                if (peak > max) max = peak;
            }
            return max;
        }
    }
}

#endregion