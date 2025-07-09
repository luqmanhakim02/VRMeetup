using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Clones meshes/materials from the “local avatar” in the scene into the
    /// NetworkObject copy that NGO spawns.  Run ONLY on the owner client.
    /// </summary>
    public class RPMAvatarMeshCloner : NetworkBehaviour
    {
        /* - How do we find the local (first-person) avatar? - */
        [Tooltip("Drag your local (first-person) RPM avatar here.  " +
                 "If left empty we search for the first object tagged <Player>.")]
        [SerializeField] private GameObject m_LocalAvatarRoot;

        /* Optional: if you want to know when the cloning finished */
        public System.Action onCloneComplete;

        /* ------------------------------------------------------------------ */

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;   // only the owning client does the heavy lifting

            if (m_LocalAvatarRoot == null)
                m_LocalAvatarRoot = GameObject.FindWithTag("Player");

            if (m_LocalAvatarRoot == null)
            {
                Debug.LogError(
                    "[RPMAvatarMeshCloner] Local avatar not found – " +
                    "assign it in Inspector or tag it LocalAvatar.");
                return;
            }

            CloneMeshes(m_LocalAvatarRoot, gameObject);

            onCloneComplete?.Invoke();
        }

        /* ------------------------------------------------------------------ */

        private static void CloneMeshes(GameObject srcRoot, GameObject dstRoot)
        {
            // Build a quick lookup: renderer name -> renderer instance on LOCAL avatar
            var srcDict = new Dictionary<string, SkinnedMeshRenderer>();
            foreach (var r in srcRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                srcDict[r.gameObject.name] = r;

            int cloned = 0;

            foreach (var dstRend in dstRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!srcDict.TryGetValue(dstRend.gameObject.name, out var srcRend))
                    continue;   // no matching part – skip

                // --- 1. Mesh
                dstRend.sharedMesh = srcRend.sharedMesh;

                // --- 2. Materials (deep copy of references so every client owns its instance)
                var mats = srcRend.materials;
                var newMats = new Material[mats.Length];
                for (int i = 0; i < mats.Length; ++i)
                    newMats[i] = mats[i];
                dstRend.materials = newMats;

                // --- 3. Blend-shape weights
                int shapeCount = srcRend.sharedMesh.blendShapeCount;
                for (int s = 0; s < shapeCount; ++s)
                    dstRend.SetBlendShapeWeight(s, srcRend.GetBlendShapeWeight(s));

                cloned++;
            }

            Debug.Log($"[RPMAvatarMeshCloner] Cloned {cloned} mesh parts " +
                      $"from <{srcRoot.name}> into network avatar.");
        }
    }
}
