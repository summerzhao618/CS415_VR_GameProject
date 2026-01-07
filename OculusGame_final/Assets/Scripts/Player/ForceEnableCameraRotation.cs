using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Ensures camera rotation (continuous turn) works even when holding objects
/// Attach to XR Origin to force turn provider to stay enabled
/// </summary>
public class ForceEnableCameraRotation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Continuous Turn Provider on XR Origin")]
    [SerializeField] private MonoBehaviour continuousTurnProvider;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private void Update()
    {
        // Ensure turn provider stays enabled regardless of grab state
        if (continuousTurnProvider != null && !continuousTurnProvider.enabled)
        {
            continuousTurnProvider.enabled = true;
            if (showDebugLogs)
            {
                Debug.Log("ForceEnableCameraRotation: Re-enabled turn provider");
            }
        }
    }

    /// <summary>
    /// Call this from Unity Editor to auto-find the turn provider
    /// </summary>
    public void AutoFindTurnProvider()
    {
        // Try to find ActionBasedContinuousTurnProvider (legacy)
        var legacyProvider = GetComponentInChildren(System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousTurnProvider, Unity.XR.Interaction.Toolkit"));

        // Try to find ContinuousTurnProvider (new)
        var newProvider = GetComponentInChildren(System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider, Unity.XR.Interaction.Toolkit"));

        if (legacyProvider != null)
        {
            continuousTurnProvider = legacyProvider as MonoBehaviour;
            Debug.Log("Found legacy ActionBasedContinuousTurnProvider");
        }
        else if (newProvider != null)
        {
            continuousTurnProvider = newProvider as MonoBehaviour;
            Debug.Log("Found new ContinuousTurnProvider");
        }
        else
        {
            Debug.LogWarning("Could not auto-find turn provider. Please assign manually.");
        }
    }
}
