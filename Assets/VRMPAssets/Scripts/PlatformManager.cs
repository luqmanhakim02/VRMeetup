using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using XRMultiplayer;
using Unity.Netcode;

public class PlatformManager : MonoBehaviour
{
    const string k_DebugPrepend = "<color=#938FFF>[Platform Manager]</color> ";

    [Header("Offline Player References")]
    [SerializeField] private GameObject vrPlayerPrefab;  // The offline VR player prefab
    [SerializeField] private GameObject desktopPlayerPrefab;  // The offline Desktop player prefab

    [Header("Network Player Prefabs")]
    [SerializeField] private GameObject networkVRPlayerPrefab;  // The networked VR player prefab
    [SerializeField] private GameObject networkDesktopPlayerPrefab;  // The networked Desktop player prefab

    [Header("Origin References")]
    [SerializeField] private GameObject vrOrigin;  // XR Interaction Setup for VR
    [SerializeField] private GameObject desktopOrigin;  // Desktop player setup

    [Header("Event Systems")]
    [SerializeField] private EventSystem vrEventSystem; // EventSystem for VR
    [SerializeField] private EventSystem desktopEventSystem; // EventSystem for Desktop

    [Header("Cameras")]
    [SerializeField] private Camera vrCamera;  // Camera for VR
    [SerializeField] private Camera desktopCamera;  // Camera for Desktop

    [Header("Network Settings")]
    [SerializeField] private NetworkManager networkManager; // Reference to the NetworkManager
    [SerializeField] private bool autoSetNetworkPrefab = true; // Automatically set network prefab on start?

    private Camera mainCamera;  // Main Camera, used for the Billboard script
    private bool isVR;

    void Start()
    {
        // Initially deactivate both EventSystems
        if (vrEventSystem != null) vrEventSystem.gameObject.SetActive(false);
        if (desktopEventSystem != null) desktopEventSystem.gameObject.SetActive(false);

        // Find network manager if not set
        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
        }

        // Instantiate the player prefabs immediately to ensure the UI is set up
        InstantiatePlayerPrefabs();

        // Remove the default EventSystem if it's present in the scene
        DestroyDefaultEventSystem();

        // Now check if the VR device is active or not
        Invoke("CheckVRDevice", 2.0f); // Optional delay if needed to ensure the system is ready
    }

    void InstantiatePlayerPrefabs()
    {
        // Active one prefabs for instance purpose
        if (vrOrigin != null) vrOrigin.SetActive(true);  // Activate the VR setup initially
        mainCamera = vrCamera;  // Set it to VR camera initially
    }

    void CheckVRDevice()
    {
        // Check if the VR device is active or not
        isVR = XRSettings.isDeviceActive;

        if (isVR)
        {
            Utils.Log($"{k_DebugPrepend}VR Found.");
            if (vrOrigin != null) vrOrigin.SetActive(true);  // Activate the VR setup
            if (desktopOrigin != null) desktopOrigin.SetActive(false);  // Deactivate the Desktop setup

            // Enable VR EventSystem, Disable Desktop EventSystem
            if (vrEventSystem != null) vrEventSystem.gameObject.SetActive(true);  // Activate the VR EventSystem
            if (desktopEventSystem != null) desktopEventSystem.gameObject.SetActive(false);  // Deactivate the Desktop EventSystem

            // Assign the VR camera to the Billboard script's m_Camera variable
            mainCamera = vrCamera;

            // Set the VR player prefab in the Network Manager
            if (networkManager != null && autoSetNetworkPrefab && networkVRPlayerPrefab != null)
            {
                SetNetworkPlayerPrefab(networkVRPlayerPrefab);
            }
        }
        else
        {
            Utils.Log($"{k_DebugPrepend}No VR Found. Desktop Mode Enabled.");
            if (vrOrigin != null) vrOrigin.SetActive(false);  // Deactivate the VR setup
            if (desktopOrigin != null) desktopOrigin.SetActive(true);  // Activate the Desktop setup

            // Enable Desktop EventSystem, Disable VR EventSystem
            if (vrEventSystem != null) vrEventSystem.gameObject.SetActive(false);  // Deactivate the VR EventSystem
            if (desktopEventSystem != null) desktopEventSystem.gameObject.SetActive(true);  // Activate the Desktop EventSystem

            // Assign the Desktop camera to the Billboard script's m_Camera variable
            mainCamera = desktopCamera;

            // Set the Desktop player prefab in the Network Manager
            if (networkManager != null && autoSetNetworkPrefab && networkDesktopPlayerPrefab != null)
            {
                SetNetworkPlayerPrefab(networkDesktopPlayerPrefab);
            }
        }

        // Pass the assigned camera to the Billboard script
        // Using FindObjectsByType with FindObjectSortMode.None to avoid unnecessary sorting
        var billboards = Object.FindObjectsByType<Billboard>(FindObjectsSortMode.None);
        foreach (var billboard in billboards)
        {
            billboard.SetCamera(mainCamera); // Custom method to set the camera reference
        }
    }

    /// <summary>
    /// Set the player prefab in the NetworkManager
    /// </summary>
    /// <param name="prefab">The player prefab to use</param>
    public void SetNetworkPlayerPrefab(GameObject prefab)
    {
        if (networkManager == null)
        {
            Debug.LogError($"{k_DebugPrepend}NetworkManager reference is missing!");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError($"{k_DebugPrepend}Player prefab is null!");
            return;
        }

        // Check if the prefab has a NetworkObject component
        if (!prefab.TryGetComponent<NetworkObject>(out _))
        {
            Debug.LogError($"{k_DebugPrepend}Player prefab does not have a NetworkObject component!");
            return;
        }

        // Set the player prefab in the NetworkManager
        Utils.Log($"{k_DebugPrepend}Setting NetworkManager player prefab to: {prefab.name}");
        networkManager.NetworkConfig.PlayerPrefab = prefab;
    }

    // Destroy the default EventSystem (if it's present)
    void DestroyDefaultEventSystem()
    {
        // Find the first EventSystem in the scene (we want the first active one)
        EventSystem defaultEventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (defaultEventSystem != null &&
           (vrEventSystem == null || defaultEventSystem != vrEventSystem) &&
           (desktopEventSystem == null || defaultEventSystem != desktopEventSystem))
        {
            // Destroy the default EventSystem to prevent multiple EventSystems in the scene
            Destroy(defaultEventSystem.gameObject);
        }
    }

    /// <summary>
    /// Returns whether the platform is using VR or not
    /// </summary>
    public bool IsVRMode()
    {
        return isVR;
    }
}