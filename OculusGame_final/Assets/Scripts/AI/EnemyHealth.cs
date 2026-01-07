using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Health system for enemies
/// Handles taking damage, death, and health management
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health points")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Current health points")]
    [SerializeField] private float currentHealth;

    [Header("Death Settings")]
    [Tooltip("Time before destroying GameObject after death")]
    [SerializeField] private float destroyDelay = 5f;

    [Tooltip("Whether to disable ragdoll on death (if applicable)")]
    [SerializeField] private bool useRagdollOnDeath = false;

    [Header("Feedback Settings")]
    [Tooltip("Material to flash when taking damage (optional)")]
    [SerializeField] private Material hitFlashMaterial;

    [Tooltip("Duration of hit flash effect")]
    [SerializeField] private float flashDuration = 0.1f;

    // Public property to check if enemy is dead
    public bool IsDead { get; private set; } = false;

    // Public properties for UI access
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    // References
    private Animator animator;
    private NavMeshAgent navAgent;
    private SkeletonEnemyAI aiController;
    private Renderer[] renderers;
    private Material[] originalMaterials;

    // Animator parameter names
    private readonly int hitTrigger = Animator.StringToHash("Hit");
    private readonly int dieTrigger = Animator.StringToHash("Die");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
        aiController = GetComponent<SkeletonEnemyAI>();

        // Get all renderers for hit flash effect
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }
    }

    private void Start()
    {
        // Initialize health to max
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage to enemy
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        // Don't take damage if already dead
        if (IsDead)
        {
            return;
        }

        // Reduce health
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        // Trigger hit animation if alive
        if (currentHealth > 0f)
        {
            if (animator != null)
            {
                animator.SetTrigger(hitTrigger);
            }

            // Flash red on hit
            if (hitFlashMaterial != null)
            {
                StartCoroutine(FlashHit());
            }
        }
        else
        {
            // Health depleted - die
            Die();
        }
    }

    /// <summary>
    /// Heal the enemy
    /// </summary>
    public void Heal(float healAmount)
    {
        if (IsDead)
        {
            return;
        }

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    /// <summary>
    /// Get current health percentage (0 to 1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Handle enemy death
    /// </summary>
    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        // Disable AI
        if (aiController != null)
        {
            aiController.enabled = false;
        }

        // Stop NavMeshAgent
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }

        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger(dieTrigger);
        }

        // Disable colliders (prevent further hits)
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Optional: Enable ragdoll physics
        if (useRagdollOnDeath)
        {
            EnableRagdoll();
        }

        // Destroy GameObject after delay
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// Enable ragdoll physics on death (if rigidbodies are set up)
    /// </summary>
    private void EnableRagdoll()
    {
        // Disable animator
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Enable all rigidbodies in children
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    /// <summary>
    /// Flash material red when hit
    /// </summary>
    private System.Collections.IEnumerator FlashHit()
    {
        // Change to hit flash material
        if (renderers != null && hitFlashMaterial != null)
        {
            foreach (Renderer rend in renderers)
            {
                rend.material = hitFlashMaterial;
            }
        }

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Restore original materials
        if (renderers != null && originalMaterials != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (i < originalMaterials.Length)
                {
                    renderers[i].material = originalMaterials[i];
                }
            }
        }
    }

    /// <summary>
    /// Optional: Instant kill for testing
    /// </summary>
    [ContextMenu("Kill Enemy")]
    public void InstantKill()
    {
        TakeDamage(currentHealth);
    }

    /// <summary>
    /// Optional: Reset health to full
    /// </summary>
    [ContextMenu("Reset Health")]
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead = false;
    }

    // Debug visualization - DISABLED to prevent errors
    // (Now using EnemyHealthBar UI component instead)
    /*
    private void OnGUI()
    {
        if (IsDead)
        {
            return;
        }

        // Show health bar above enemy (for debugging)
        if (Camera.main == null)
        {
            return;
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

        if (screenPos.z > 0)
        {
            float healthPercentage = GetHealthPercentage();

            // Background bar
            GUI.color = Color.black;
            GUI.Box(new Rect(screenPos.x - 52, Screen.height - screenPos.y - 12, 104, 14), "");

            // Health bar
            GUI.color = Color.Lerp(Color.red, Color.green, healthPercentage);
            GUI.Box(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 10, 100 * healthPercentage, 10), "");

            GUI.color = Color.white;
        }
    }
    */
}
