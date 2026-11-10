using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatBotHandler : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Animator animator;
    public Transform target;
    public HealthBar healthBar;
    public Canvas mainCanvas;
    public RandomAudioPool randomAudioPool;

    [Header("General Settings")]
    public bool allowInput = true;
    public bool invinsible;
    public bool isChainsaw;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    [Header("Dash Settings")]
    public float dashSpeedMultiplier = 7f; // NEW: Adjustable dash speed
    public float dashCooldown = 3f; // NEW: Cooldown between dashes
    public float approachDashChance = 0.05f; // NEW: Reduced from 0.1f
    public float retreatDashChance = 0.1f; // NEW: Reduced from 0.2f

    [Header("Combo Settings")]
    public float inputCooldown = 0.2f;
    public float doubleTapWindow = 0.3f;
    public float singleTapDelay = 0.2f;
    public int weaponType; // 0 none, 1 hand, 2 axe

    [Header("Attack Durations (movement locks)")]
    public float[] attackMovementLocks;
    public float[] attackAnimResetDurations;

    [Header("Move Durations (jump/crouch/dash locks)")]
    public float[] moveMovementLocks;
    public float[] moveAnimResetDurations;

    [Header("Health System")]
    public float health = 100f;
    public float lowHealthThreshold = 30f;

    [Header("Base Damage Values")]
    public float headBaseDamage = 50f;
    public float bodyBaseDamage = 10f;
    public float limbBaseDamage = 3f;
    public float defaultBaseDamage = 5f;

    [Header("Bonus Damage Chances (0-1)")]
    [Range(0f, 1f)] public float limbBonusChance = 0.1f;
    [Range(0f, 1f)] public float bodyBonusChance = 0.25f;
    [Range(0f, 1f)] public float headBonusChance = 0.3f;

    [Header("Bonus Damage Amounts")]
    public float limbBonusDamage = 3f;
    public float bodyBonusDamage = 5f;
    public float headBonusDamage = 3f;

    [Header("AI Settings")]
    public float detectionRange = 8f;
    public float attackRange = 2.2f;
    public float preferredCombatDistance = 2.5f;
    public float retreatDistance = 4f;
    public float aggressionLevel = 0.4f;
    public float reactionTime = 0.3f;
    public LayerMask enemyLayers = -1;

    [Header("Attack Resistance System")]
    [Range(0f, 1f)] public float attackResistanceRate = 0.3f; // 0 = no resistance, 1 = always resist
    [Range(0f, 1f)] public float headAttackResistanceModifier = 0.5f; // Head attacks are harder to resist
    [Range(0f, 1f)] public float sweepAttackResistanceModifier = 0.7f; // Sweeps are harder to resist
    public bool showResistanceDebug = true;

    [Header("Anti-Spam Defense System")]
    [Range(0f, 1f)] public float spamResistanceBonus = 0.4f; // Extra resistance when being spammed
    public float spamDetectionWindow = 2f; // How long to track attacks for spam detection
    public int spamThreshold = 4; // How many attacks in window = spam
    public float defensiveModeTimeout = 3f; // How long defensive mode lasts

    [Header("Tactical AI Settings")]
    [Range(0f, 1f)] public float headHunterChance = 0.6f;
    [Range(0f, 1f)] public float sweepOpportunityChance = 0.8f;
    public float flightAttackDistance = 3.5f;
    public float backflipSetupDistance = 4f;

    // AI State
    private enum BotState { Idle, Approaching, Attacking, Retreating, Circling, Defending, Stunned, SettingUpFlip }
    private BotState currentState = BotState.Idle;

    // Internal variables
    private Vector2 moveAxis;
    private Vector2 currentMoveInput;
    private bool isMovementLocked;
    private bool moveConsumed = false;
    private bool isAttacking;

    // Dash state tracking (matches CombatInputHandler system)
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashDirection = 0f;
    private float dashDuration = 0f;
    private float lastDashTime = 0f; // NEW: Track last dash time for cooldown

    private float lastKickTapTime;
    private float lastKickActionTime;
    private float lastPunchTapTime;
    private float lastPunchActionTime;
    private Vector2 lastMoveDirection;
    private float lastMoveTime;

    // AI Decision variables
    private float lastStateChangeTime;
    private float lastAttackTime;
    private float lastDamageTime;
    private bool isLowHealth => health <= lowHealthThreshold;
    private float nextDecisionTime;
    private List<Transform> nearbyEnemies = new List<Transform>();
    private int comboCounter = 0;
    private float comboCooldown = 2.5f;

    // Tactical variables
    private bool targetIsStunned = false;
    private float lastTargetDamageTime = 0f;
    private Vector3 setupPosition;

    // Anti-spam defense system
    private List<float> recentAttackTimes = new List<float>();
    private bool isBeingSpammed = false;
    private float lastSpamDetectionTime = 0f;
    private bool defensiveModeActive = false;
    private float defensiveModeStartTime = 0f;

    bool Hbonus, Bbonus, Lbonus;
    private int faceStyle;
    [Header("Hit Part References")]
    public Transform headTransform;
    public Transform bodyTransform;
    public Transform limbTransform;
    public Transform legLimbTransform;
    [Header("FX")]
    public GameObject POWhitEffectPrefab;
    public GameObject SMASHhitEffectPrefab;
    public GameObject headshotTXT;
    public GameObject axeWeaponhand, CHainsawweaponhand;

    private void Start()
    {
        healthBar.Initialize(health);

        if (target == null)
            FindNearestTarget();

        nextDecisionTime = Time.time + reactionTime;
        animator.SetInteger("weaponID", weaponType);
        
        // Weapon setup matching player system
        if (weaponType == 1)
        {
            axeWeaponhand.SetActive(false);
            CHainsawweaponhand.SetActive(false);
        }
        if (weaponType == 2)
        {
            if(isChainsaw)
            {
                axeWeaponhand.SetActive(false);
                CHainsawweaponhand.SetActive(true);
            }
            else
            {
                axeWeaponhand.SetActive(true);
                CHainsawweaponhand.SetActive(false);
            }
        }
    }

    private void Update()
    {
        healthBar.UpdateHealth(health);

        FaceTarget();
        UpdateSpamDetection();
        
        if (!allowInput) return;

        UpdateEnemyDetection();
        UpdateTargetState();

        if (Time.time >= nextDecisionTime && currentState != BotState.Stunned)
        {
            MakeDecision();
            nextDecisionTime = Time.time + reactionTime + Random.Range(0f, 0.2f);
        }

        ExecuteCurrentBehavior();
    }

    private void FixedUpdate()
    {
        // If stunned, override all other movement to prevent jittering
        if (currentState == BotState.Stunned)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        // Handle dashing first (highest priority) - matches CombatInputHandler
        if (isDashing)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, dashDirection * moveSpeed * dashSpeedMultiplier);
            dashTimer += Time.fixedDeltaTime;

            if (dashTimer >= dashDuration)
            {
                isDashing = false;
                dashTimer = 0f;
                isAttacking = false;
            }
            return;
        }

        // Match player's hvmove animation reset logic
        if (!allowInput && (animator.GetInteger("hvmove") == 7 || animator.GetInteger("hvmove") == -7)) 
        {
            animator.SetInteger("hvmove", 0);
        }

        if (!allowInput) return;

        // Normal movement logic (matches CombatInputHandler with new animation system)
        if (!isMovementLocked && !isAttacking && !isDashing)
        {
            if (currentMoveInput != Vector2.zero)
            {
                int multiplier = Mathf.RoundToInt(Mathf.Clamp(currentMoveInput.x, -1f, 1f));
                animator.SetInteger("hvmove", 7 * faceStyle * multiplier);
                Vector3 moveDir = new Vector3(0, 0, currentMoveInput.x);
                rb.velocity = new Vector3(0, rb.velocity.y, moveDir.z * moveSpeed);
            }
            else
            {
                animator.SetInteger("hvmove", 0);
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
        }
        else if (isMovementLocked && !isAttacking)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    // ---------------- DASH CONTROL SYSTEM ----------------
    private bool CanDash()
    {
        return Time.time - lastDashTime >= dashCooldown;
    }

    private void TryDash(Vector2 direction, float chance)
    {
        if (CanDash() && Random.value < chance && Time.time - lastMoveTime > doubleTapWindow)
        {
            SimulateDoubleTap(direction);
            lastDashTime = Time.time; // Record dash time for cooldown
        }
    }

    // ---------------- TACTICAL AI ----------------
    private void UpdateTargetState()
    {
        if (target == null) return;

        if (Time.time - lastTargetDamageTime < 2f)
        {
            targetIsStunned = true;
        }
        else
        {
            targetIsStunned = false;
        }
    }

    // ---------------- ANTI-SPAM DEFENSE SYSTEM ----------------
    private void UpdateSpamDetection()
    {
        // Clean up old attack times
        for (int i = recentAttackTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time - recentAttackTimes[i] > spamDetectionWindow)
            {
                recentAttackTimes.RemoveAt(i);
            }
        }

        // Check if we're being spammed
        bool wasBeingSpammed = isBeingSpammed;
        isBeingSpammed = recentAttackTimes.Count >= spamThreshold;

        // If spam just started, activate defensive mode
        if (isBeingSpammed && !wasBeingSpammed)
        {
            defensiveModeActive = true;
            defensiveModeStartTime = Time.time;
            if (showResistanceDebug)
            {
                Debug.Log("Bot: SPAM DETECTED! Activating defensive mode.");
            }
        }

        // Check if defensive mode should end
        if (defensiveModeActive && Time.time - defensiveModeStartTime > defensiveModeTimeout)
        {
            defensiveModeActive = false;
            if (showResistanceDebug)
            {
                Debug.Log("Bot: Defensive mode ended.");
            }
        }
    }

    private void RecordAttackReceived()
    {
        recentAttackTimes.Add(Time.time);
        
        // Immediate defensive reaction if spam threshold reached
        if (recentAttackTimes.Count >= spamThreshold && currentState != BotState.Defending)
        {
            currentState = BotState.Retreating;
            if (showResistanceDebug)
            {
                Debug.Log("Bot: Switching to retreat due to spam!");
            }
        }
    }

    private void UpdateEnemyDetection()
    {
        nearbyEnemies.Clear();
        Collider[] detectedColliders = Physics.OverlapSphere(transform.position, detectionRange, enemyLayers);

        foreach (Collider col in detectedColliders)
        {
            if (col.transform != transform && col.CompareTag("Player"))
            {
                nearbyEnemies.Add(col.transform);
            }
        }

        if (nearbyEnemies.Count > 0)
        {
            target = GetClosestEnemy();
        }
    }

    private Transform GetClosestEnemy()
    {
        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform enemy in nearbyEnemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    private void MakeDecision()
    {
        if (target == null)
        {
            currentState = BotState.Idle;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        float timeSinceLastAttack = Time.time - lastAttackTime;
        float timeSinceLastDamage = Time.time - lastDamageTime;

        // Emergency retreat if low health
        if (isLowHealth && timeSinceLastDamage < 3f)
        {
            currentState = BotState.Retreating;
            return;
        }

        // Tactical decision making
        if (distanceToTarget > detectionRange)
        {
            currentState = BotState.Idle;
        }
        else if (distanceToTarget > backflipSetupDistance && Random.value < 0.3f)
        {
            currentState = BotState.SettingUpFlip;
        }
        else if (distanceToTarget > attackRange)
        {
            currentState = BotState.Approaching;
        }
        else if (distanceToTarget <= attackRange)
        {
            if (targetIsStunned && Random.value < sweepOpportunityChance)
            {
                currentState = BotState.Attacking;
            }
            else if (timeSinceLastAttack > comboCooldown)
            {
                float attackChance = aggressionLevel * (health / 100f) * 0.7f;
                if (Random.value < attackChance)
                {
                    currentState = BotState.Attacking;
                }
                else if (Random.value < 0.4f)
                {
                    currentState = BotState.Circling;
                }
                else
                {
                    currentState = BotState.Defending;
                }
            }
            else
            {
                if (Random.value < 0.6f)
                    currentState = BotState.Circling;
                else
                    currentState = BotState.Defending;
            }
        }

        lastStateChangeTime = Time.time;
    }

    private void ExecuteCurrentBehavior()
    {
        switch (currentState)
        {
            case BotState.Idle:
                ExecuteIdle();
                break;
            case BotState.Approaching:
                ExecuteApproach();
                break;
            case BotState.Attacking:
                ExecuteAttack();
                break;
            case BotState.Retreating:
                ExecuteRetreat();
                break;
            case BotState.Circling:
                ExecuteCircling();
                break;
            case BotState.Defending:
                ExecuteDefending();
                break;
            case BotState.Stunned:
                ExecuteStunned();
                break;
            case BotState.SettingUpFlip:
                ExecuteFlipSetup();
                break;
        }
    }

    private void ExecuteIdle()
    {
        currentMoveInput = Vector2.zero;
        moveAxis = Vector2.zero;
        FindNearestTarget();
    }

    private void ExecuteApproach()
    {
        if (target == null) return;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        currentMoveInput = new Vector2(directionToTarget.z, 0);
        moveAxis = currentMoveInput;

        // Controlled dash usage with cooldown
        TryDash(moveAxis, approachDashChance);
    }

    private void ExecuteAttack()
    {
        if (target == null) return;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        currentMoveInput = new Vector2(directionToTarget.z, 0);
        moveAxis = currentMoveInput;

        ChooseTacticalAttack();
        lastAttackTime = Time.time;
        comboCounter++;

        if (comboCounter >= 2)
        {
            comboCounter = 0;
            currentState = BotState.Circling;
        }
    }

    private void ExecuteRetreat()
    {
        if (target == null) return;

        Vector3 directionAwayFromTarget = (transform.position - target.position).normalized;
        currentMoveInput = new Vector2(directionAwayFromTarget.z, 0);
        moveAxis = currentMoveInput;

        // Controlled dash usage with cooldown
        TryDash(moveAxis, retreatDashChance);

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > retreatDistance)
        {
            currentState = BotState.Circling;
        }
    }

    private void ExecuteCircling()
    {
        if (target == null) return;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Vector3 circleDirection = Vector3.Cross(directionToTarget, Vector3.up).normalized;

        if (Random.value < 0.5f) circleDirection *= -1;

        currentMoveInput = new Vector2(circleDirection.z, 0);
        moveAxis = currentMoveInput;
    }

    private void ExecuteDefending()
    {
        currentMoveInput = Vector2.zero;
        moveAxis = Vector2.zero;

        if (Random.value < 0.05f)
        {
            currentState = BotState.Attacking;
        }
    }

    private void ExecuteStunned()
    {
        // Force zero movement during stun to prevent jittering
        currentMoveInput = Vector2.zero;
        moveAxis = Vector2.zero;
        isDashing = false;
        isAttacking = false;
        
        // Ensure rigidbody velocity is zeroed for X and Z (keep Y for gravity)
        if (rb != null)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    private void ExecuteFlipSetup()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget < backflipSetupDistance - 0.5f)
        {
            Vector3 directionAwayFromTarget = (transform.position - target.position).normalized;
            currentMoveInput = new Vector2(directionAwayFromTarget.z, 0);
            moveAxis = currentMoveInput;
        }
        else if (distanceToTarget > backflipSetupDistance + 0.5f)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            currentMoveInput = new Vector2(directionToTarget.z, 0);
            moveAxis = currentMoveInput;
        }
        else
        {
            currentState = BotState.Attacking;
        }
    }

    // ---------------- TACTICAL ATTACK SELECTION ----------------
    private void ChooseTacticalAttack()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // OVERPOWERED ATTACKS - Use strategically
        if (targetIsStunned && Random.value < sweepOpportunityChance)
        {
            Debug.Log("Bot: Using sweep kick on vulnerable target!");
            SimulateKick(new Vector2(0, -1));
            return;
        }

        if (distanceToTarget >= flightAttackDistance && Random.value < headHunterChance)
        {
            Debug.Log("Bot: Using fly kick to target head!");
            SimulateKick(new Vector2(0, 1));
            return;
        }

        if (distanceToTarget >= backflipSetupDistance - 1f && Random.value < headHunterChance)
        {
            Debug.Log("Bot: Using backflip kick from distance!");
            SimulateDoubleTapKick(new Vector2(-1, 0));
            return;
        }

        if (distanceToTarget >= 2.5f && Random.value < headHunterChance)
        {
            Debug.Log("Bot: Using crescent kick for head damage!");
            SimulateDoubleTapKick(new Vector2(-1, -1));
            return;
        }

        if (distanceToTarget <= 2f && Random.value < 0.6f)
        {
            Debug.Log("Bot: Using shin kick for quick pressure!");
            SimulateKick(new Vector2(1, -1));
            return;
        }

        // WEAPON-SPECIFIC TACTICS
        if (weaponType == 2)
        {
            if (Random.value < 0.5f)
                SimulateDoubleTapPunch(new Vector2(1, 0));
            else
                SimulatePunch(new Vector2(1, 0));
        }
        else if (weaponType == 1)
        {
            if (comboCounter == 0)
                SimulatePunch(new Vector2(1, 0));
            else
                SimulateKick(new Vector2(1, 0));
        }
        else
        {
            SimulateKick(new Vector2(1, 0));
        }
    }

    // ---------------- INPUT SIMULATION ----------------
    private void SimulateKick(Vector2 direction)
    {
        moveAxis = direction;
        currentMoveInput = direction;
        StartCoroutine(DelayedAction(() => OnKickSimulated(), 0.05f));
    }

    private void SimulateDoubleTapKick(Vector2 direction)
    {
        moveAxis = direction;
        currentMoveInput = direction;
        StartCoroutine(DoubleTapSequence(() => OnKickSimulated()));
    }

    private void SimulatePunch(Vector2 direction)
    {
        moveAxis = direction;
        currentMoveInput = direction;
        StartCoroutine(DelayedAction(() => OnPunchSimulated(), 0.05f));
    }

    private void SimulateDoubleTapPunch(Vector2 direction)
    {
        moveAxis = direction;
        currentMoveInput = direction;
        StartCoroutine(DoubleTapSequence(() => OnPunchSimulated()));
    }

    private void SimulateDoubleTap(Vector2 direction)
    {
        moveAxis = direction;
        currentMoveInput = direction;
        OnDoubleTapMove(direction);
    }

    private IEnumerator DoubleTapSequence(System.Action action)
    {
        action.Invoke();
        lastKickTapTime = Time.time;
        lastPunchTapTime = Time.time;

        yield return new WaitForSeconds(doubleTapWindow * 0.5f);

        action.Invoke();
    }

    private IEnumerator DelayedAction(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }

    private void FindNearestTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (GameObject player in players)
        {
            if (player.transform == transform) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = player.transform;
            }
        }

        target = closestTarget;
    }

    // ---------------- ADAPTED ORIGINAL METHODS ----------------
    private void OnKickSimulated()
    {
        if (!allowInput) return;

        float now = Time.time;

        if (now - lastKickActionTime < inputCooldown && !(now - lastKickTapTime <= doubleTapWindow))
            return;

        if (now - lastKickTapTime <= doubleTapWindow)
        {
            if (moveAxis.x > 0 && moveAxis.y == 0)
            {
                TriggerAttack("kickID", 6, attackMovementLocks[5], attackAnimResetDurations[5]);
            }
            if (moveAxis.x < 0 && moveAxis.y < 0)
            {
                TriggerAttack("kickID", 8, attackMovementLocks[6], attackAnimResetDurations[6]);
            }
            if (moveAxis.x < 0 && moveAxis.y == 0)
            {
                TriggerAttack("kickID", 9, attackMovementLocks[7], attackAnimResetDurations[7]);
            }

            lastKickActionTime = now;
            lastKickTapTime = 0f;
            moveConsumed = true;
            return;
        }

        if (moveAxis.x > 0 && moveAxis.y == 0)
        {
            TriggerAttack("kickID", 1, attackMovementLocks[0], attackAnimResetDurations[0]);
            moveConsumed = true;
        }
        if (moveAxis.x > 0 && moveAxis.y < 0)
        {
            TriggerAttack("kickID", 2, attackMovementLocks[1], attackAnimResetDurations[1]);
            moveConsumed = true;
        }
        if (moveAxis.x == 0 && moveAxis.y < 0)
        {
            TriggerAttack("kickID", 3, attackMovementLocks[2], attackAnimResetDurations[2]);
            moveConsumed = true;
        }
        if ((moveAxis.x < 0) && (moveAxis.y == 0 || moveAxis.y < 0))
        {
            TriggerAttack("kickID", 4, attackMovementLocks[3], attackAnimResetDurations[3]);
            moveConsumed = true;
        }
        if (moveAxis.y > 0)
        {
            TriggerAttack("kickID", 5, attackMovementLocks[4], attackAnimResetDurations[4]);
            moveConsumed = true;
        }

        lastKickActionTime = now;
        lastKickTapTime = now;
    }

    private void OnPunchSimulated()
    {
        if (!allowInput) return;

        float now = Time.time;

        if (now - lastPunchActionTime < inputCooldown && !(now - lastPunchTapTime <= doubleTapWindow))
            return;

        if (now - lastPunchTapTime <= doubleTapWindow)
        {
            // Match player's new double-tap punch logic that includes fly kick
            if (moveAxis.y > 0) { TriggerAttack("kickID", 5, attackMovementLocks[4], attackAnimResetDurations[4]); moveConsumed = true; }

            if (moveAxis.x > 0 && moveAxis.y == 0)
            {
                TriggerAttack("punchID", 1, attackMovementLocks[8], attackAnimResetDurations[8]);
                if (weaponType == 1)
                {
                    StartCoroutine(BotPublicMove(.5f, 4, faceStyle));
                }
                if (weaponType == 2)
                {
                    StartCoroutine(BotPublicMove(3, 8, faceStyle));
                }
            }
            lastPunchActionTime = now;
            lastPunchTapTime = 0f;
            moveConsumed = true;
            return;
        }

        if (moveAxis.x > 0 && moveAxis.y == 0)
        {
            TriggerAttack("punchID", 2, attackMovementLocks[9], attackAnimResetDurations[9]);
            if (weaponType == 1)
            {
                StartCoroutine(BotPublicMove(.5f, 4, faceStyle));
            }
            if (weaponType == 2)
            {
                // StartCoroutine(BotPublicMove(1, 7, faceStyle));
            }
        }
        if (moveAxis.x < 0 && moveAxis.y == 0)
        {
            TriggerAttack("punchID", 3, attackMovementLocks[10], attackAnimResetDurations[10]);
            if (weaponType == 1)
            {
                StartCoroutine(BotPublicMove(.5f, 4, faceStyle));
            }
            if (weaponType == 2)
            {
                // StartCoroutine(BotPublicMove(1, 7, faceStyle));
            }
        }

        lastPunchActionTime = now;
        lastPunchTapTime = now;
        moveConsumed = true;
    }

    private void OnDoubleTapMove(Vector2 dir)
    {
        if (dir.x > 0 && dir.y == 0)
        {
            TriggerCombo("hvmove", 3, moveMovementLocks[2], moveAnimResetDurations[2]);
            StartDash(1f, moveMovementLocks[2]);
        }
        else if (dir.x < 0 && dir.y == 0)
        {
            TriggerCombo("hvmove", 4, moveMovementLocks[3], moveAnimResetDurations[3]);
            StartDash(-1f, moveMovementLocks[3]);
        }
    }

    private void StartDash(float direction, float duration)
    {
        isDashing = true;
        isAttacking = true;
        dashDirection = direction;
        dashDuration = duration;
        dashTimer = 0f;
    }

    IEnumerator BotPublicMove(float lifetime, float speed, int zAxis)
    {
        float timer = 0f;
        Vector3 moveDir = new Vector3(0, 0, zAxis);
        isAttacking = true;

        while (timer < lifetime)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, moveDir.z * speed);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(.4f);
        isAttacking = false;
    }

    // Updated to match player's optimized parameter management
    private void TriggerAttack(string param, int value, float lockDuration, float resetDuration)
    {
        allowInput = false;
        // Set the parameter immediately
        animator.SetInteger(param, value);
        
        // Schedule reset using animation events instead of coroutines
        StartCoroutine(LockMovement(lockDuration));
        
        // Use Invoke for cleaner parameter reset (better than coroutines)
        Invoke(nameof(ResetAllAttackParams), resetDuration);
    }
    
    private void TriggerCombo(string param, int value, float lockDuration, float resetDuration)
    {
        allowInput = false;
        
        animator.SetInteger(param, value);
        StartCoroutine(LockMovement(lockDuration));
        Invoke(nameof(ResetAllMoveParams), resetDuration);
    }
    
    // Reset all attack parameters at once to prevent conflicts
    private void ResetAllAttackParams()
    {
        allowInput = true;
        
        animator.SetInteger("kickID", 0);
        animator.SetInteger("punchID", 0);
    }
    
    // Reset movement parameters
    private void ResetAllMoveParams()
    {
        allowInput = true;
        
        animator.SetInteger("hvmove", 0);
    }

    private IEnumerator LockMovement(float duration)
    {
        isMovementLocked = true;
        yield return new WaitForSeconds(duration);
        isMovementLocked = false;
    }

    // Enhanced hit system matching CombatInputHandler
    public enum HitPart { Head, Body, Limb, LegLimb }

    public void ApplyHit(HitPart part, bool sweep)
    {
        // Record this attack for spam detection
        RecordAttackReceived();
        
        // ATTACK RESISTANCE SYSTEM - Bot can completely ignore attacks
        float resistanceChance = CalculateResistanceChance(part, sweep);
        
        if (Random.value < resistanceChance)
        {
            if (showResistanceDebug)
            {
                Debug.Log($"Bot: Resisted {part} attack! (Resistance: {resistanceChance * 100:F0}%)");
            }
            
            // DEFENSIVE COUNTER-ATTACK when resisting spam
            if (isBeingSpammed && Random.value < 0.3f)
            {
                StartCoroutine(DelayedCounterAttack(0.1f));
            }
            
            return;
        }
        
        float damage = defaultBaseDamage;
        float bonusDuration = 1f;
        Vector3 hitPosition = Vector3.zero; // stores where we got hit

        // stop rb instantly on hit
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Stop any ongoing movement to prevent jittering
        StopAllCoroutines();
        isDashing = false;
        isAttacking = false;
        isMovementLocked = false;
        currentMoveInput = Vector2.zero;
        moveAxis = Vector2.zero;

        switch (part)
        {
            case HitPart.Head:
                damage = headBaseDamage;
                hitPosition = headTransform.position; // world coordinates of head
                if (Random.value < headBonusChance)
                {
                    damage += headBonusDamage;
                    bonusDuration = 3.4f;
                }
                else
                {
                    bonusDuration = 3.4f;
                }
                TriggerCombo("hvmove", 6, moveMovementLocks[6], moveAnimResetDurations[6]);
                invinsible = true;
                Debug.Log("Bot Head hit at " + hitPosition);
                break;

            case HitPart.Body:
                damage = bodyBaseDamage;
                hitPosition = bodyTransform.position; // world coordinates of body
                if (Random.value < bodyBonusChance || isDashing)
                {
                    damage += bodyBonusDamage;
                    bonusDuration = 1;
                }
                else
                {
                    bonusDuration = 1;
                }
                TriggerCombo("hvmove", 5, moveMovementLocks[5], moveAnimResetDurations[5]);
                StartCoroutine(BotPublicMove(.5f, 5, -faceStyle));
                invinsible = true;
                Debug.Log("Bot Body hit at " + hitPosition);
                break;

            case HitPart.Limb:
                damage = limbBaseDamage;
                hitPosition = limbTransform.position; // use correct limb reference
                if (Random.value < limbBonusChance || isDashing)
                {
                    damage += limbBonusDamage;
                    bonusDuration = 1;
                    TriggerCombo("hvmove", 5, moveMovementLocks[5], moveAnimResetDurations[5]);
                }
                else
                {
                    bonusDuration = 1;
                    TriggerCombo("hvmove", 5, moveMovementLocks[5], moveAnimResetDurations[5]);
                    StartCoroutine(BotPublicMove(.5f, 5, -faceStyle));
                }
                invinsible = true;
                Debug.Log("Bot Leg hit at " + hitPosition);
                break;

            case HitPart.LegLimb:
                damage = limbBaseDamage;
                hitPosition = legLimbTransform.position; // use correct limb reference
                if (Random.value < limbBonusChance || sweep || isDashing)
                {
                    damage += limbBonusDamage;
                    bonusDuration = 3.4f;
                    TriggerCombo("hvmove", 6, moveMovementLocks[6], moveAnimResetDurations[6]);
                }
                else
                {
                    bonusDuration = 1;
                    TriggerCombo("hvmove", 5, moveMovementLocks[5], moveAnimResetDurations[5]);
                    StartCoroutine(BotPublicMove(.5f, 5, -faceStyle));
                }
                invinsible = true;
                Debug.Log("Bot Limb hit at " + hitPosition);
                break;

            default:
                Debug.Log("Bot Other hit!");
                break;
        }

        allowInput = false;
        currentState = BotState.Stunned;
        lastTargetDamageTime = Time.time;
        
        // Call helper function with the hit position
        ReportHitPosition(part, hitPosition);
        
        // Use separate timers for stun recovery and animation reset to prevent conflicts
        StartCoroutine(StunRecovery(bonusDuration));
        TakeDamage(damage);
    }
    private void ReportHitPosition(HitPart part, Vector3 position)
    {
        randomAudioPool.PlayRandom();
        GameObject effect;
        GameObject effectHEADTEXT;
        Debug.Log($"Hit {part} at world position {position}");
        if (part == HitPart.Head)
        {
            // World-space effect
            effect = Instantiate(SMASHhitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 0.6f);

            // UI effect
            effectHEADTEXT = Instantiate(headshotTXT, mainCanvas.transform);

            // ? Use RectTransform anchoredPosition, not localPosition
            RectTransform rect = effectHEADTEXT.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(620.7f, -80.6f);

            Destroy(effectHEADTEXT, 1f);
        }


        else
        {
            effect = Instantiate(POWhitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 0.6f);

        }
    }
    // Calculate resistance chance based on attack type and bot state
    private float CalculateResistanceChance(HitPart part, bool sweep)
    {
        float baseResistance = attackResistanceRate;
        
        // SPAM RESISTANCE - Major bonus when being spammed
        if (isBeingSpammed || defensiveModeActive)
        {
            baseResistance += spamResistanceBonus;
            if (showResistanceDebug)
            {
                Debug.Log($"Bot: Spam resistance active! +{spamResistanceBonus * 100:F0}% resistance");
            }
        }
        
        // Modify resistance based on attack type
        switch (part)
        {
            case HitPart.Head:
                baseResistance *= headAttackResistanceModifier; // Head attacks harder to resist
                break;
            case HitPart.Body:
                // Normal resistance rate
                break;
            case HitPart.Limb:
            case HitPart.LegLimb:
                if (sweep)
                {
                    baseResistance *= sweepAttackResistanceModifier; // Sweeps harder to resist
                }
                else
                {
                    baseResistance *= 1.2f; // Regular limb attacks easier to resist
                }
                break;
        }
        
        // Increase resistance when low health (desperation mode)
        if (isLowHealth)
        {
            baseResistance *= 1.3f;
        }
        
        // Increase resistance based on aggression level
        float aggressionBonus = aggressionLevel * 0.5f;
        baseResistance += aggressionBonus;
        
        // Cap resistance at 90% maximum (increased from 85% for spam protection)
        return Mathf.Clamp(baseResistance, 0f, 0.9f);
    }

    private IEnumerator DelayedCounterAttack(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (target != null && allowInput)
        {
            if (showResistanceDebug)
            {
                Debug.Log("Bot: Counter-attacking after resisting spam!");
            }
            
            // Quick counter attack
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= attackRange)
            {
                SimulateKick(new Vector2(1, 0)); // Quick jab
            }
            else
            {
                SimulateKick(new Vector2(0, 1)); // Fly kick to close distance
            }
        }
    }

    IEnumerator StunRecovery(float delay)
    {
        Hbonus = false;
        Bbonus = false;
        Lbonus = false;
        
        // Ensure the bot stays down for the full duration
        yield return new WaitForSeconds(delay);
        
        // Only recover if still in stunned state (prevents interruption by other hits)
        if (currentState == BotState.Stunned)
        {
            allowInput = true;
            invinsible = false;
            currentState = BotState.Idle;
            
            // Brief delay before resuming normal behavior to prevent immediate jittering
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void TakeDamage(float amount)
    {
        CombatBotHandler rootHandler = GetComponentInParent<CombatBotHandler>();
        if (rootHandler != null && rootHandler != this)
        {
            rootHandler.TakeDamage(amount);
            return;
        }

        health -= amount;
        health = Mathf.Clamp(health, 0, 1000);
        lastDamageTime = Time.time;
        Debug.Log("Bot Health: " + health);

        if (Random.value < 0.4f && currentState != BotState.Stunned)
        {
            currentState = BotState.Retreating;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        StopAllCoroutines();
        CancelInvoke();
        animator.SetBool("isBlock", false);
        animator.SetInteger("kickID", 800);
        animator.SetInteger("punchID", 800);
        animator.SetInteger("hvmove", 800);

        allowInput = false;
        Debug.Log("Bot is dead!");
        currentState = BotState.Idle;
        enabled = false;
    }
    private void FaceTarget()
    {
        if (target == null) return;

        Vector3 directionToTarget = target.position - transform.position;

        // Only check the Z axis since movement is along it
        if (directionToTarget.z > 0)
        {
            // Face forward (rotation = 0)
            transform.rotation = Quaternion.Euler(0, 0, 0);
            faceStyle=1;
        }
        else if (directionToTarget.z < 0)
        {
            // Face backward (rotation = 180)
            transform.rotation = Quaternion.Euler(0, 180, 0);
            faceStyle = -1;

        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, preferredCombatDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, flightAttackDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, backflipSetupDistance);
    }
}
