using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that follows enemy and displays current health
/// Attach to a Canvas as child of enemy
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the enemy's health component")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Tooltip("The fill image that represents health amount")]
    [SerializeField] private Image healthBarFill;

    [Tooltip("Background image (optional)")]
    [SerializeField] private Image healthBarBackground;

    [Header("Visual Settings")]
    [Tooltip("Color when health is full")]
    [SerializeField] private Color fullHealthColor = Color.green;

    [Tooltip("Color when health is low")]
    [SerializeField] private Color lowHealthColor = Color.red;

    [Tooltip("Health percentage threshold for low health color (0-1)")]
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Tooltip("Hide health bar when at full health")]
    [SerializeField] private bool hideWhenFull = true;

    [Tooltip("Hide health bar when dead")]
    [SerializeField] private bool hideWhenDead = true;

    private Camera mainCamera;
    private Canvas canvas;
    private RectTransform fillRectTransform;
    private float maxFillWidth;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        // Auto-find enemy health if not assigned
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        // Get fill rect transform for Unity 6 width-based filling
        if (healthBarFill != null)
        {
            fillRectTransform = healthBarFill.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        // Find main camera
        mainCamera = Camera.main;

        // Setup canvas for world space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }

        // Store max width for health bar
        if (fillRectTransform != null)
        {
            maxFillWidth = fillRectTransform.rect.width;
        }
    }

    private void Update()
    {
        if (enemyHealth == null || healthBarFill == null)
        {
            return;
        }

        // Update health bar fill amount using width (Unity 6 compatible)
        float healthPercentage = enemyHealth.GetHealthPercentage();

        // Update fill width instead of fillAmount
        if (fillRectTransform != null)
        {
            RectTransform parentRect = fillRectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                float parentWidth = parentRect.rect.width;
                fillRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth * healthPercentage);
            }
        }

        // Update color based on health
        healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage / lowHealthThreshold);

        // Hide/Show based on settings
        bool shouldShow = true;

        if (hideWhenFull && healthPercentage >= 1f)
        {
            shouldShow = false;
        }

        if (hideWhenDead && enemyHealth.IsDead)
        {
            shouldShow = false;
        }

        canvas.enabled = shouldShow;

        // Face camera (billboard effect)
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// Call this to setup the health bar after instantiation
    /// </summary>
    public void Initialize(EnemyHealth health)
    {
        enemyHealth = health;
    }
}
