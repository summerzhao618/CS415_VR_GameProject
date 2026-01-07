using UnityEngine;

/// <summary>
/// Makes UI Canvas follow the VR camera with offset
/// Attach this to your GameUI Canvas for VR mode
/// </summary>
public class VRUIFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Camera to follow (auto-finds Main Camera if not set)")]
    [SerializeField] private Transform targetCamera;

    [Tooltip("Offset from camera position")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0f, 2f);

    [Tooltip("Should the UI follow camera rotation")]
    [SerializeField] private bool followRotation = true;

    [Tooltip("Smooth follow speed (0 = instant, higher = slower)")]
    [SerializeField] private float followSpeed = 5f;

    [Tooltip("Only update position, keep UI always facing camera")]
    [SerializeField] private bool billboardMode = true;

    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponent<Canvas>();

        // Auto-find camera if not assigned
        if (targetCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                targetCamera = mainCam.transform;
            }
            else
            {
                Debug.LogWarning("VRUIFollow: No camera found! Please assign manually.");
            }
        }

        // Ensure canvas is in World Space mode for VR
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            Debug.Log("VRUIFollow: Canvas set to World Space mode");
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        // Calculate target position
        Vector3 targetPosition = targetCamera.position + targetCamera.TransformDirection(positionOffset);

        // Smooth follow or instant
        if (followSpeed > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }
        else
        {
            transform.position = targetPosition;
        }

        // Handle rotation
        if (billboardMode)
        {
            // Always face the camera
            transform.LookAt(transform.position + targetCamera.rotation * Vector3.forward,
                           targetCamera.rotation * Vector3.up);
        }
        else if (followRotation)
        {
            // Follow camera rotation
            transform.rotation = targetCamera.rotation;
        }
    }

    // Visualize UI position in editor
    private void OnDrawGizmosSelected()
    {
        if (targetCamera != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 targetPos = targetCamera.position + targetCamera.TransformDirection(positionOffset);
            Gizmos.DrawWireCube(targetPos, new Vector3(0.5f, 0.3f, 0.01f));
            Gizmos.DrawLine(targetCamera.position, targetPos);
        }
    }
}
