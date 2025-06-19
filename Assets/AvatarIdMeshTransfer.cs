using System;
using UnityEngine;
using ReadyPlayerMe.Core;

public class AvatarIdMeshTransfer : MonoBehaviour
{
    [Header("Avatar Configuration")]
    public string avatarId; // your avatar ID
    public AvatarConfig avatarConfig;

    [Header("Target Avatar Rig")]
    public GameObject targetAvatarRoot;

    private AvatarObjectLoader avatarLoader;

    [ContextMenu("Load and Transfer Mesh")]
    public void LoadAndTransferAvatarMesh()
    {
        if (string.IsNullOrEmpty(avatarId))
        {
            Debug.LogError("[AvatarIdMeshTransfer] Avatar ID is empty!");
            return;
        }

        if (avatarConfig == null)
        {
            Debug.LogError("[AvatarIdMeshTransfer] AvatarConfig is missing!");
            return;
        }

        if (targetAvatarRoot == null)
        {
            Debug.LogError("[AvatarIdMeshTransfer] Target avatar root is missing!");
            return;
        }

        // Use only the avatar ID—SDK knows how to resolve it
        string avatarRef = avatarId;
        Debug.Log($"[AvatarIdMeshTransfer] Loading avatar ID: {avatarRef}");

        avatarLoader = new AvatarObjectLoader
        {
            AvatarConfig = avatarConfig
        };

        avatarLoader.OnCompleted += OnAvatarLoaded;
        avatarLoader.OnFailed += OnAvatarLoadFailed;
        avatarLoader.LoadAvatar(avatarRef);
    }

    private void OnAvatarLoaded(object sender, CompletionEventArgs args)
    {
        Debug.Log("[AvatarIdMeshTransfer] Avatar loaded—transferring mesh!");

        AvatarMeshHelper.TransferMesh(args.Avatar, targetAvatarRoot);
        Destroy(args.Avatar); // cleanup
    }

    private void OnAvatarLoadFailed(object sender, FailureEventArgs args)
    {
        Debug.LogError($"[AvatarIdMeshTransfer] Load failed: {args.Type} – {args.Message}");
    }
}
