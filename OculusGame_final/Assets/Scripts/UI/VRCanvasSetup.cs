using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Automatically configures a Canvas for VR interaction
/// Attach to GameOverCanvas to ensure buttons work with VR controllers
/// </summary>
[RequireComponent(typeof(Canvas))]
public class VRCanvasSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("Automatically find and assign Main Camera")]
    [SerializeField] private bool autoFindCamera = true;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        SetupCanvas();
        SetupEventSystem();
    }

    private void SetupCanvas()
    {
        // Ensure World Space mode for VR
        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // Find and assign camera if needed
        if (autoFindCamera && canvas.worldCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                canvas.worldCamera = mainCam;
            }
        }

        // Remove old Graphic Raycaster if it exists
        GraphicRaycaster oldRaycaster = GetComponent<GraphicRaycaster>();
        if (oldRaycaster != null)
        {
            // Check if it's the XR version
            string typeName = oldRaycaster.GetType().Name;
            if (!typeName.Contains("Tracked"))
            {
                DestroyImmediate(oldRaycaster);
            }
        }

        // Add Tracked Device Graphic Raycaster for VR if not present
        if (GetComponent<GraphicRaycaster>() == null)
        {
            // Try to add TrackedDeviceGraphicRaycaster (XR Toolkit component)
            System.Type trackedRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");

            if (trackedRaycasterType != null)
            {
                gameObject.AddComponent(trackedRaycasterType);
            }
            else
            {
                // Fallback to regular GraphicRaycaster
                GraphicRaycaster raycaster = gameObject.AddComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = true;
                raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            }
        }
    }

    private void SetupEventSystem()
    {
        // Check if EventSystem exists in scene
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            // Create EventSystem for UI interaction
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();

            // Try to add XR UI Input Module
            System.Type xrInputModuleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");

            if (xrInputModuleType != null)
            {
                eventSystemObj.AddComponent(xrInputModuleType);
            }
            else
            {
                // Fallback to Standalone Input Module
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }
    }

    // Helper method to verify setup in editor
    private void OnValidate()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }
    }
}
