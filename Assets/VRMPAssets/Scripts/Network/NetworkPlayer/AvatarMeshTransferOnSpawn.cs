using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace XRMultiplayer
{
    /// <summary>
    /// OWNER-only: copies meshes/materials from the first-person RPM avatar
    /// named “RPM_Template_Avatar_XR” (even if inactive) into this NetworkObject,
    /// then hides the local avatar so only the network one remains visible.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class AvatarMeshTransferOnSpawn : NetworkBehaviour
    {
        [Header("Where to put the copied renderers")]
        [SerializeField] private Transform avatarRoot;      // e.g. XRINetworkPlayer/AvatarRoot

        [Header("Local avatar name (inactive OK)")]
        [SerializeField] private string localAvatarName = "RPM_Local_Avatar_XR";

        [Header("Cleanup")]
        [Tooltip("Delete any placeholder children under AvatarRoot before copying.")]
        [SerializeField] private bool clearOldChildren = true;

        /* --------------------------------------------------------------------- */

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;                       // only the local owner copies

            var localAvatar = FindInactiveOrActive(localAvatarName);
            if (localAvatar == null)
            {
                Debug.LogError($"[AvatarMeshTransfer] Local avatar \"{localAvatarName}\" not found.");
                return;
            }

            if (avatarRoot == null)
            {
                Debug.LogError("[AvatarMeshTransfer] AvatarRoot not assigned on network prefab.");
                return;
            }

            if (clearOldChildren)
            {
                foreach (Transform child in avatarRoot) Destroy(child.gameObject);
            }

            TransferMeshes(localAvatar, avatarRoot.gameObject);

            // Finally: hide the local avatar so the player only sees the network version
            localAvatar.SetActive(false);
        }

        /* --------------------------------------------------------------------- */

        private static GameObject FindInactiveOrActive(string goName)
        {
            var active = GameObject.Find(goName);
            if (active) return active;

            var any = Resources.FindObjectsOfTypeAll<Transform>()
                               .FirstOrDefault(t => t.name == goName);
            return any ? any.gameObject : null;
        }

        private static void TransferMeshes(GameObject srcAvatar, GameObject dstRoot)
        {
            var srcMeshes = srcAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var dstMeshes = dstRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            int count = 0;
            foreach (var dst in dstMeshes)
            {
                var src = srcMeshes.FirstOrDefault(s => s.name == dst.name);
                if (src == null) continue;

                dst.sharedMesh = src.sharedMesh;
                dst.materials = src.materials;

                // Copy blend-shape weights
                for (int i = 0; i < src.sharedMesh.blendShapeCount; ++i)
                    dst.SetBlendShapeWeight(i, src.GetBlendShapeWeight(i));

                dst.shadowCastingMode = ShadowCastingMode.On;
                dst.receiveShadows = true;
                count++;
            }

            Debug.Log($"[AvatarMeshTransfer] Copied {count} mesh parts into network avatar.");
        }
    }
}
