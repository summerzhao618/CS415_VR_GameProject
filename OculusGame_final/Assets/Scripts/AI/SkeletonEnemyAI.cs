using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Skeleton Enemy AI with Patrol, Chase, and Attack behaviors
/// Designed for use with the Skeleton_110 prefab
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class SkeletonEnemyAI : MonoBehaviour
{
    #region AI States
    public enum AIState
    {
        Patrol,
        Chase,
        Attack
    }

    [Header("Current State (Debug)")]
    [SerializeField] private AIState currentState = AIState.Patrol;
    #endregion

    #region Patrol Settings
    [Header("Patrol Configuration")]
    [Tooltip("Radius around spawn point for patrol area")]
    [SerializeField] private float patrolRadius = 10f;

    [Tooltip("Time to wait at each patrol waypoint")]
    [SerializeField] private float patrolWaitTime = 2f;

    [Tooltip("Movement speed during patrol")]
    [SerializeField] private float patrolSpeed = 1.5f;

    private Vector3 spawnPosition;
    private float patrolWaitTimer = 0f;
    private bool isWaiting = false;
    #endregion

    #region Chase Settings
    [Header("Chase Configuration")]
    [Tooltip("Movement speed during chase")]
    [SerializeField] private float chaseSpeed = 3.5f;

    [Tooltip("Distance at which enemy detects player")]
    [SerializeField] private float detectionRange = 15f;

    [Tooltip("Distance at which enemy stops chasing")]
    [SerializeField] private float loseTargetDistance = 20f;

    [Tooltip("Field of view angle in degrees (180 = half circle)")]
    [SerializeField] private float fieldOfView = 120f;

    [Tooltip("Layer mask for obstacles that block line of sight")]
    [SerializeField] private LayerMask obstacleLayer;
    #endregion

    #region Attack Settings
    [Header("Attack Configuration")]
    [Tooltip("Distance at which enemy starts attacking")]
    [SerializeField] private float attackRange = 5f;

    [Tooltip("Time between attacks")]
    [SerializeField] private float attackCooldown = 2f;

    [Tooltip("Projectile prefab to spawn")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Transform where projectile spawns (usually near hand)")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Tooltip("Speed at which projectile is launched")]
    [SerializeField] private float projectileSpeed = 15f;

    [Tooltip("How fast enemy rotates to face player")]
    [SerializeField] private float rotationSpeed = 5f;

    private float attackCooldownTimer = 0f;
    private bool isAttacking = false;
    #endregion

    #region References
    [Header("References")]
    [Tooltip("Reference to the player transform (camera rig for VR)")]
    [SerializeField] private Transform player;

    private NavMeshAgent navAgent;
    private Animator animator;
    private EnemyHealth healthComponent;

    // Animator parameter names
    private readonly int speedParam = Animator.StringToHash("Speed");
    private readonly int attackTrigger = Animator.StringToHash("Attack");
    #endregion

    #region Initialization
    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        healthComponent = GetComponent<EnemyHealth>();

        spawnPosition = transform.position;

        // Set initial NavMeshAgent settings
        navAgent.speed = patrolSpeed;
        navAgent.stoppingDistance = 0.5f;
        navAgent.autoBraking = true;
    }

    private void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            // Try to find VR camera rig first
            GameObject cameraRig = GameObject.Find("XR Origin");
            if (cameraRig == null)
            {
                cameraRig = GameObject.Find("Camera");
            }

            if (cameraRig != null)
            {
                player = cameraRig.transform;
            }
        }

        // Create projectile spawn point if not assigned
        if (projectileSpawnPoint == null)
        {
            GameObject spawnPointObj = new GameObject("ProjectileSpawnPoint");
            spawnPointObj.transform.SetParent(transform);
            spawnPointObj.transform.localPosition = new Vector3(0f, 1.5f, 0.5f); // Near chest/hand level
            projectileSpawnPoint = spawnPointObj.transform;
        }

        // Start in patrol state
        SetState(AIState.Patrol);
    }
    #endregion

    #region State Machine
    private void Update()
    {
        // Don't update AI if dead
        if (healthComponent != null && healthComponent.IsDead)
        {
            navAgent.isStopped = true;
            animator.SetFloat(speedParam, 0f);
            return;
        }

        // Update cooldown timer
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // State machine logic
        switch (currentState)
        {
            case AIState.Patrol:
                PatrolBehavior();
                break;
            case AIState.Chase:
                ChaseBehavior();
                break;
            case AIState.Attack:
                AttackBehavior();
                break;
        }

        // Update animator speed parameter based on NavMeshAgent velocity
        float speed = navAgent.velocity.magnitude;
        animator.SetFloat(speedParam, speed);
    }

    private void SetState(AIState newState)
    {
        // Exit current state
        switch (currentState)
        {
            case AIState.Patrol:
                isWaiting = false;
                patrolWaitTimer = 0f;
                break;
            case AIState.Attack:
                isAttacking = false;
                break;
        }

        // Enter new state
        currentState = newState;

        switch (newState)
        {
            case AIState.Patrol:
                navAgent.speed = patrolSpeed;
                navAgent.stoppingDistance = 0.5f;
                navAgent.isStopped = false;
                SetNewPatrolDestination();
                break;
            case AIState.Chase:
                navAgent.speed = chaseSpeed;
                navAgent.stoppingDistance = attackRange * 0.8f;
                navAgent.isStopped = false;
                break;
            case AIState.Attack:
                navAgent.isStopped = true;
                break;
        }
    }
    #endregion

    #region Patrol Behavior
    private void PatrolBehavior()
    {
        // Check for player detection
        if (DetectPlayer())
        {
            SetState(AIState.Chase);
            return;
        }

        // Handle waiting at waypoint
        if (isWaiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                isWaiting = false;
                SetNewPatrolDestination();
            }
            return;
        }

        // Check if reached destination
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f)
            {
                // Start waiting at waypoint
                isWaiting = true;
                patrolWaitTimer = patrolWaitTime;
            }
        }
    }

    private void SetNewPatrolDestination()
    {
        Vector3 randomPoint = GetRandomPatrolPoint();
        navAgent.SetDestination(randomPoint);
    }

    private Vector3 GetRandomPatrolPoint()
    {
        // Generate random point within patrol radius
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += spawnPosition;

        // Find nearest valid NavMesh position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // Fallback to spawn position if no valid point found
        return spawnPosition;
    }
    #endregion

    #region Chase Behavior
    private void ChaseBehavior()
    {
        if (player == null)
        {
            SetState(AIState.Patrol);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is too far away
        if (distanceToPlayer > loseTargetDistance || !HasLineOfSight())
        {
            SetState(AIState.Patrol);
            return;
        }

        // Check if in attack range
        if (distanceToPlayer <= attackRange)
        {
            SetState(AIState.Attack);
            return;
        }

        // Chase player
        navAgent.SetDestination(player.position);
    }
    #endregion

    #region Attack Behavior
    private void AttackBehavior()
    {
        if (player == null)
        {
            SetState(AIState.Patrol);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If player moved too far, return to chase
        if (distanceToPlayer > attackRange * 1.5f)
        {
            SetState(AIState.Chase);
            return;
        }

        // If lost line of sight, return to chase
        if (!HasLineOfSight())
        {
            SetState(AIState.Chase);
            return;
        }

        // Face the player
        FaceTarget(player.position);

        // Attack if cooldown is ready and not already attacking
        if (attackCooldownTimer <= 0f && !isAttacking)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        isAttacking = true;
        attackCooldownTimer = attackCooldown;

        // Trigger attack animation
        animator.SetTrigger(attackTrigger);

        // Launch projectile after a short delay (simulating animation wind-up)
        Invoke(nameof(LaunchProjectile), 0.5f);

        // Reset attacking flag after animation completes
        Invoke(nameof(ResetAttacking), 1.0f);
    }

    private void ResetAttacking()
    {
        isAttacking = false;
    }

    private void LaunchProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || player == null)
        {
            return;
        }

        // Instantiate projectile at spawn point
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);

        // Ignore collision between projectile and this enemy
        Collider projectileCollider = projectile.GetComponent<Collider>();
        Collider[] enemyColliders = GetComponentsInChildren<Collider>();

        if (projectileCollider != null)
        {
            foreach (Collider enemyCol in enemyColliders)
            {
                Physics.IgnoreCollision(projectileCollider, enemyCol);
            }
        }

        // Calculate direction to player
        Vector3 direction = (player.position - projectileSpawnPoint.position).normalized;

        // Get projectile component and set velocity
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, projectileSpeed);
        }
        else
        {
            // Fallback: use Rigidbody if no script
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep rotation on horizontal plane only

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }
    #endregion

    #region Detection
    private bool DetectPlayer()
    {
        if (player == null)
        {
            return false;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is within detection range
        if (distanceToPlayer > detectionRange)
        {
            return false;
        }

        // Check if player is within field of view
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > fieldOfView / 2f)
        {
            return false;
        }

        // Check line of sight
        return HasLineOfSight();
    }

    private bool HasLineOfSight()
    {
        if (player == null)
        {
            return false;
        }

        Vector3 directionToPlayer = player.position - transform.position;

        // Raycast to check for obstacles
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1f, directionToPlayer.normalized, out hit, detectionRange))
        {
            // Check if we hit the player
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmosSelected()
    {
        // Draw patrol radius
        Gizmos.color = Color.blue;
        Vector3 patrolCenter = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(patrolCenter, patrolRadius);

        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw field of view
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfView / 2f, Vector3.up) * transform.forward * detectionRange;
            Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfView / 2f, Vector3.up) * transform.forward * detectionRange;

            Gizmos.DrawLine(transform.position, transform.position + fovLine1);
            Gizmos.DrawLine(transform.position, transform.position + fovLine2);
        }

        // Draw line to player if detected
        if (Application.isPlaying && player != null && currentState != AIState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, player.position);
        }
    }
    #endregion
}
