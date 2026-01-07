using UnityEngine;

/// <summary>
/// Projectile fired by enemies
/// Handles movement, collision, damage, and auto-destruction
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Damage dealt to player on hit")]
    [SerializeField] private float damage = 10f;

    [Tooltip("Projectile lifetime before auto-destruction")]
    [SerializeField] private float lifeTime = 10f;

    [Tooltip("Tag of the player object")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Collision radius for easier hits")]
    [SerializeField] private float collisionRadius = 0.5f;

    [Header("Visual Settings")]
    [Tooltip("Particle effect to spawn on impact (optional)")]
    [SerializeField] private GameObject impactEffect;

    [Tooltip("Whether to destroy projectile on any collision")]
    [SerializeField] private bool destroyOnAnyHit = true;

    private Rigidbody rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Configure rigidbody for better collision detection
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Ensure collider is set to trigger and has proper size
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;

            // Expand sphere collider for easier hits
            SphereCollider sphereCol = col as SphereCollider;
            if (sphereCol != null)
            {
                sphereCol.radius = collisionRadius;
            }
        }
    }

    private void Start()
    {
        // Destroy projectile after lifetime expires
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        // Additional raycast check for fast-moving projectiles
        if (hasHit || rb == null) return;

        // Check if we're about to hit the player with a sphere cast
        Vector3 velocity = rb.linearVelocity;
        float distance = velocity.magnitude * Time.fixedDeltaTime;

        if (Physics.SphereCast(transform.position, collisionRadius, velocity.normalized, out RaycastHit hit, distance))
        {
            // Check if we hit the player
            if (hit.collider.CompareTag(playerTag))
            {
                HandlePlayerHit(hit.collider.gameObject);
            }
            else
            {
                PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    HandlePlayerHit(health.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Initialize projectile with direction and speed
    /// Called by SkeletonEnemyAI when spawning projectile
    /// </summary>
    public void Initialize(Vector3 direction, float speed)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;

            // Rotate projectile to face direction of travel
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Prevent multiple hits
        if (hasHit)
        {
            return;
        }

        // Check if hit the player
        if (other.CompareTag(playerTag))
        {
            HandlePlayerHit(other.gameObject);
            return;
        }
        else
        {
            // Check if this is part of the player (even without tag)
            PlayerHealth healthInParent = other.GetComponentInParent<PlayerHealth>();
            if (healthInParent != null)
            {
                HandlePlayerHit(healthInParent.gameObject);
                return;
            }
        }

        // Handle hit on other objects (walls, obstacles, etc.)
        if (destroyOnAnyHit)
        {
            HandleGeneralHit();
        }
    }

    private void HandlePlayerHit(GameObject player)
    {
        hasHit = true;

        // Try to find and damage player health component
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
        else
        {
            // Fallback: try to find health component in parent
            playerHealth = player.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"EnemyProjectile hit player but no PlayerHealth component found on {player.name}");
            }
        }

        // Spawn impact effect
        SpawnImpactEffect();

        // Destroy projectile
        Destroy(gameObject);
    }

    private void HandleGeneralHit()
    {
        hasHit = true;

        // Spawn impact effect
        SpawnImpactEffect();

        // Destroy projectile
        Destroy(gameObject);
    }

    private void SpawnImpactEffect()
    {
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Clean up effect after 2 seconds
        }
    }

    // Gizmo debug visualization removed to prevent visual clutter
}
