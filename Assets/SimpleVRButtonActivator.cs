using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class SimpleVRButtonActivator : MonoBehaviour
{
    public GameObject panel;
    private InputDevice rightController;
    private bool wasPressedLastFrame = false;

    void Start()
    {
        // Find right-hand controller
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);

        if (devices.Count > 0)
            rightController = devices[0];
    }

    void Update()
    {
        if (!rightController.isValid)
        {
            // Reacquire the device if lost
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, new List<InputDevice> { rightController });
        }

        // Check if the A button (primary button) is pressed
        if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPressed))
        {
            if (isPressed && !wasPressedLastFrame)
            {
                // Toggle panel on press down
                if (panel != null)
                    panel.SetActive(!panel.activeSelf);
            }

            wasPressedLastFrame = isPressed;
        }
    }
}
