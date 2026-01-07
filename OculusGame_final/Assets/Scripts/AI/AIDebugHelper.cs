using UnityEngine;
using TMPro;

/// <summary>
/// Debug helper to visualize AI state and detection
/// Attach to Skeleton enemy to see what's happening
/// </summary>
public class AIDebugHelper : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Show debug messages in console")]
    [SerializeField] private bool showConsoleDebug = true;

    [Tooltip("Show debug text above enemy")]
    [SerializeField] private bool showDebugText = true;

    [Tooltip("Show detection gizmos in Scene view")]
    [SerializeField] private bool showGizmos = true;

    private SkeletonEnemyAI aiController;
    private TextMeshPro debugText;
    private Transform player;

    private void Start()
    {
        aiController = GetComponent<SkeletonEnemyAI>();

        if (aiController == null)
        {
            Debug.LogError("AIDebugHelper: No SkeletonEnemyAI found on this object!");
            enabled = false;
            return;
        }

        // Create debug text
        if (showDebugText)
        {
            CreateDebugText();
        }

        // Find player
        GameObject xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin != null)
        {
            player = xrOrigin.transform;
            if (showConsoleDebug)
            {
                Debug.Log($"AIDebugHelper: Found player at {xrOrigin.name}");
            }
        }
        else
        {
            Debug.LogWarning("AIDebugHelper: Cannot find XR Origin!");
        }
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                player = xrOrigin.transform;
            }
        }

        UpdateDebugInfo();
    }

    private void CreateDebugText()
    {
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 3f, 0);
        textObj.transform.localRotation = Quaternion.identity;

        debugText = textObj.AddComponent<TextMeshPro>();
        debugText.fontSize = 3;
        debugText.alignment = TextAlignmentOptions.Center;
        debugText.color = Color.yellow;

        // Make it face camera
        textObj.AddComponent<Billboard>();
    }

    private void UpdateDebugInfo()
    {
        if (aiController == null || player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // Get current state via reflection (since it's private)
        var stateField = typeof(SkeletonEnemyAI).GetField("currentState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        string currentState = "Unknown";
        if (stateField != null)
        {
            var stateValue = stateField.GetValue(aiController);
            currentState = stateValue.ToString();
        }

        // Update debug text
        if (debugText != null)
        {
            debugText.text = $"State: {currentState}\nDist: {distance:F1}m";
        }

        // Console debug every 2 seconds
        if (showConsoleDebug && Time.frameCount % 120 == 0)
        {
            Debug.Log($"[{gameObject.name}] State: {currentState} | Distance to player: {distance:F1}m | Position: {transform.position}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || player == null)
        {
            return;
        }

        // Draw line to player
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);

        // Draw distance text
        Vector3 midpoint = (transform.position + player.position) / 2f + Vector3.up * 2f;
        float distance = Vector3.Distance(transform.position, player.position);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(midpoint, $"{distance:F1}m");
#endif
    }
}

/// <summary>
/// Simple billboard component to make text face camera
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }
}
