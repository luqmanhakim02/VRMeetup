using Unity.Netcode;
using Unity.XR.CoreUtils;
using RootMotion.FinalIK;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Binds a VRIK solver to head & hand targets and hooks them
    /// to the local XR rig on the owning client.
    /// </summary>
    public class VRIKTargetBinder : NetworkBehaviour
    {
        [Header("Solver")]
        [SerializeField] VRIK vrik;                       // VRIK component on your RPM mesh

        [Header("Tracking Targets (placeholders in prefab)")]
        [SerializeField] Transform headTarget;
        [SerializeField] Transform leftHandTarget;
        [SerializeField] Transform rightHandTarget;

        public override void OnNetworkSpawn()
        {
            // 1. Plug the placeholders into the solver (runs on every client)
            vrik.solver.spine.headTarget = headTarget;
            vrik.solver.leftArm.target = leftHandTarget;
            vrik.solver.rightArm.target = rightHandTarget;

            // 2. If this is *my* player, parent the targets to my XR rig
            if (IsOwner)
            {
                XROrigin rig = FindFirstObjectByType<XROrigin>();
                if (rig == null)
                {
                    Debug.LogError("No XROrigin found in scene; VRIK targets not bound.");
                    return;
                }

                headTarget.SetParent(rig.Camera.transform, false);
                leftHandTarget.SetParent(rig.transform.Find("Camera Offset/LeftHandTracker/LeftHandTrackerIK"), false);
                rightHandTarget.SetParent(rig.transform.Find("Camera Offset/RightHandTracker/RightHandTrackerIK"), false);

                // Tell XRINetworkPlayer which origins to copy each frame
                var netPlayer = GetComponent<XRINetworkPlayer>();
                if (netPlayer != null)
                    netPlayer.SetHandOrigins(leftHandTarget, rightHandTarget);
            }

            // 3. Re-init the solver now that targets are correctly placed
            vrik.AutoDetectReferences();          // keeps it safe if prefab changes
            vrik.solver.Initiate(vrik.transform); // fixes twisted pose on first frame
        }
    }
}
