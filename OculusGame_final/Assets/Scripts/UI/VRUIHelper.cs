using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Helper script to ensure VR UI interactions work properly
/// Automatically sets up the correct raycaster and input module for VR
/// Attach this to any Canvas that needs VR interaction
/// </summary>
[RequireComponent(typeof(Canvas))]
public class VRUIHelper : MonoBehaviour
{
    [Header("VR Setup")]
    [Tooltip("Auto-configure canvas for VR interaction")]
    [SerializeField] private bool autoConfigureOnStart = true;

    [Tooltip("Distance for ray interactions")]
    [SerializeField] private float raycastDistance = 10f;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        if (autoConfigureOnStart)
        {
            ConfigureForVR();
        }
    }

    /// <summary>
    /// Configure canvas for VR interaction
    /// </summary>
    public void ConfigureForVR()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        // Set canvas to World Space for VR
        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            Debug.Log("VRUIHelper: Set canvas to WorldSpace mode");
        }

        // Find and assign main camera
        if (canvas.worldCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
                Debug.Log("VRUIHelper: Assigned main camera to canvas");
            }
        }

        // Setup the correct graphic raycaster for XR
        SetupGraphicRaycaster();

        // Ensure EventSystem exists with XR input module
        EnsureXREventSystem();

        Debug.Log("VRUIHelper: Canvas configured for VR interaction");
    }

    /// <summary>
    /// Setup the appropriate graphic raycaster for VR
    /// </summary>
    private void SetupGraphicRaycaster()
    {
        // Remove standard GraphicRaycaster if it exists
        GraphicRaycaster standardRaycaster = GetComponent<GraphicRaycaster>();

        // Try to get XR raycaster (from XR Interaction Toolkit)
        var xrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");

        if (xrRaycasterType != null)
        {
            // Check if XR raycaster already exists
            Component xrRaycaster = GetComponent(xrRaycasterType);

            if (xrRaycaster == null)
            {
                // Remove standard raycaster
                if (standardRaycaster != null)
                {
                    DestroyImmediate(standardRaycaster);
                    Debug.Log("VRUIHelper: Removed standard GraphicRaycaster");
                }

                // Add XR raycaster
                xrRaycaster = gameObject.AddComponent(xrRaycasterType);
                Debug.Log("VRUIHelper: Added TrackedDeviceGraphicRaycaster for XR");
            }
        }
        else
        {
            // XR Interaction Toolkit not available, use standard raycaster
            if (standardRaycaster == null)
            {
                standardRaycaster = gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("VRUIHelper: Added standard GraphicRaycaster");
            }

            // Configure for VR
            standardRaycaster.ignoreReversedGraphics = true;
            standardRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }
    }

    /// <summary>
    /// Ensure EventSystem exists with proper XR input module
    /// </summary>
    private void EnsureXREventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            // Create new EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            Debug.Log("VRUIHelper: Created EventSystem");
        }

        // Try to add XR UI Input Module if available
        var xrInputModuleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");

        if (xrInputModuleType != null)
        {
            Component xrInputModule = eventSystem.GetComponent(xrInputModuleType);

            if (xrInputModule == null)
            {
                // Remove old input modules
                StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    DestroyImmediate(oldModule);
                }

                // Add XR input module
                eventSystem.gameObject.AddComponent(xrInputModuleType);
                Debug.Log("VRUIHelper: Added XRUIInputModule");
            }
        }
        else
        {
            // Fallback to standard input module
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("VRUIHelper: Added StandaloneInputModule");
            }
        }
    }

    /// <summary>
    /// Enable all button interactions (useful if buttons become unresponsive)
    /// </summary>
    [ContextMenu("Enable All Buttons")]
    public void EnableAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            button.interactable = true;
        }
        Debug.Log($"VRUIHelper: Enabled {buttons.Length} buttons");
    }

    /// <summary>
    /// Check if canvas is properly configured for VR
    /// </summary>
    [ContextMenu("Check VR Configuration")]
    public void CheckConfiguration()
    {
        Debug.Log("=== VR UI Configuration Check ===");

        if (canvas == null)
        {
            Debug.LogError("Canvas component not found!");
            return;
        }

        Debug.Log($"Canvas Render Mode: {canvas.renderMode}");
        Debug.Log($"World Camera Assigned: {canvas.worldCamera != null}");

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        Debug.Log($"Has GraphicRaycaster: {raycaster != null}");

        var xrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (xrRaycasterType != null)
        {
            Component xrRaycaster = GetComponent(xrRaycasterType);
            Debug.Log($"Has TrackedDeviceGraphicRaycaster: {xrRaycaster != null}");
        }

        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        Debug.Log($"EventSystem exists: {eventSystem != null}");

        if (eventSystem != null)
        {
            var xrInputModuleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
            if (xrInputModuleType != null)
            {
                Component xrInputModule = eventSystem.GetComponent(xrInputModuleType);
                Debug.Log($"Has XRUIInputModule: {xrInputModule != null}");
            }
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        Debug.Log($"Total buttons found: {buttons.Length}");

        int interactableButtons = 0;
        foreach (Button button in buttons)
        {
            if (button.interactable)
            {
                interactableButtons++;
            }
        }
        Debug.Log($"Interactable buttons: {interactableButtons}");

        Debug.Log("=== End Configuration Check ===");
    }

    /// <summary>
    /// Force unpause the game (if buttons become unresponsive due to Time.timeScale)
    /// </summary>
    [ContextMenu("Force Unpause")]
    public void ForceUnpause()
    {
        Time.timeScale = 1f;
        Debug.Log("VRUIHelper: Game unpaused (Time.timeScale = 1)");
    }
}
