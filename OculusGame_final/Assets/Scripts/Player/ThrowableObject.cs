using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Makes an object grabbable and throwable in VR
/// Damages enemies when thrown at them
/// Unity 6 compatible version
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrowableObject : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Base damage when thrown")]
    [SerializeField] private float baseDamage = 30f;

    [Tooltip("Minimum velocity needed to cause damage")]
    [SerializeField] private float minDamageVelocity = 2f;

    [Tooltip("Multiply damage by velocity")]
    [SerializeField] private bool velocityScalesDamage = true;

    [Tooltip("Maximum damage possible")]
    [SerializeField] private float maxDamage = 100f;

    [Header("Physics Settings")]
    [Tooltip("Mass of the object")]
    [SerializeField] private float mass = 1f;

    [Tooltip("How bouncy the object is (0-1)")]
    [SerializeField] private float bounciness = 0.3f;

    [Header("Visual Feedback")]
    [Tooltip("Effect to spawn on impact")]
    [SerializeField] private GameObject impactEffect;

    [Tooltip("Sound when hitting enemy")]
    [SerializeField] private AudioClip hitSound;

    [Tooltip("Sound when hitting environment")]
    [SerializeField] private AudioClip bounceSound;

    [Header("Respawn Settings")]
    [Tooltip("Auto-respawn object after being thrown")]
    [SerializeField] private bool autoRespawn = true;

    [Tooltip("Time before respawning")]
    [SerializeField] private float respawnDelay = 5f;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private bool hasHitEnemy = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Configure rigidbody
        rb.mass = mass;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound

        // Store spawn position
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Start()
    {
        // Try to get XRGrabInteractable if it exists
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to grab events
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.AddListener(OnReleased);

            // Fix attachment settings for hand grab (not ray grab)
            grabInteractable.useDynamicAttach = true;
            grabInteractable.matchAttachPosition = true;
            grabInteractable.matchAttachRotation = false;
            grabInteractable.snapToColliderVolume = false;

            // Disable rotation tracking so joystick can rotate camera instead
            grabInteractable.trackRotation = false;
            grabInteractable.smoothRotation = false;
            grabInteractable.throwOnDetach = true; // Enable throwing
        }

        // Setup physics material for bounciness
        SetupPhysicsMaterial();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void SetupPhysicsMaterial()
    {
        PhysicsMaterial material = new PhysicsMaterial("ThrowablePhysics");
        material.bounciness = bounciness;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        material.bounceCombine = PhysicsMaterialCombine.Maximum;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.material = material;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Object was just released/thrown
        hasHitEnemy = false;

        // Auto-respawn after delay if enabled
        if (autoRespawn)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactVelocity = collision.relativeVelocity.magnitude;

        // Check if hit enemy
        EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = collision.gameObject.GetComponentInParent<EnemyHealth>();
        }

        if (enemyHealth != null && !hasHitEnemy)
        {
            HandleEnemyHit(enemyHealth, impactVelocity, collision.GetContact(0).point);
        }
        else
        {
            // Hit environment
            HandleEnvironmentHit(impactVelocity, collision.GetContact(0).point);
        }
    }

    private void HandleEnemyHit(EnemyHealth enemy, float velocity, Vector3 hitPoint)
    {
        // Only damage if thrown fast enough
        if (velocity < minDamageVelocity)
        {
            return;
        }

        hasHitEnemy = true;

        // Calculate damage
        float damage = baseDamage;

        if (velocityScalesDamage)
        {
            // Scale damage by velocity (faster throw = more damage)
            float velocityMultiplier = Mathf.Clamp(velocity / 10f, 0.5f, 3f);
            damage = baseDamage * velocityMultiplier;
            damage = Mathf.Clamp(damage, baseDamage * 0.5f, maxDamage);
        }

        // Apply damage
        enemy.TakeDamage(damage);

        // Visual feedback
        SpawnImpactEffect(hitPoint);

        // Audio feedback
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

    private void HandleEnvironmentHit(float velocity, Vector3 hitPoint)
    {
        // Play bounce sound if impact is strong enough
        if (velocity > 2f && bounceSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bounceSound, Mathf.Clamp01(velocity / 10f));
        }
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void Respawn()
    {
        // Don't respawn if currently being held
        if (grabInteractable.isSelected)
        {
            return;
        }

        // Reset position and rotation
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // Reset physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset state
        hasHitEnemy = false;
    }

    /// <summary>
    /// Manually reset object to spawn position
    /// </summary>
    public void ResetToSpawn()
    {
        CancelInvoke(nameof(Respawn));
        Respawn();
    }

    /// <summary>
    /// Set new spawn position (useful for pickup points)
    /// </summary>
    public void SetSpawnPosition(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 spawnPos = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(spawnPos, 0.2f);
        Gizmos.DrawLine(spawnPos, spawnPos + Vector3.up * 0.5f);
    }
}
