using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatInputHandler : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Animator animator;
    public Transform target;
    public HealthBar healthBar;
    public Canvas mainCanvas;
    public RandomAudioPool randomAudioPool;

    [Header("General Settings")]
    public bool allowInput = true; // 👈 NEW: toggle input on/off
    public bool invinsible;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

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

    [Header("Health system")]
    public float health = 100;

    [Header("Base Damage Values")]
    public float headBaseDamage = 50f;
    public float bodyBaseDamage = 10f;
    public float limbBaseDamage = 3f;
    public float defaultBaseDamage = 5f;

    [Header("Bonus Damage Chances (0–1)")]
    [Range(0f, 1f)] public float limbBonusChance = 0.1f;
    [Range(0f, 1f)] public float bodyBonusChance = 0.25f;
    [Range(0f, 1f)] public float headBonusChance = 0.3f;

    [Header("Bonus Damage Amounts")]
    public float limbBonusDamage = 3f;
    public float bodyBonusDamage = 5f;
    public float headBonusDamage = 3f;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction kickAction;
    private InputAction punchAction;

    private Vector2 moveAxis;
    private Vector2 currentMoveInput;
    private bool isMovementLocked;
    private bool moveConsumed = false;
    private bool isAttacking;
    public bool isChainsaw;
    // Dash state tracking (non-coroutine system)
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashDirection = 0f;
    private float dashDuration = 0f;
    public float dashintennsity = 0f;
    public float dashCoolDown = 1f;
    public bool allowDash;
    private float lastKickTapTime;
    private float lastKickActionTime;
    private float lastPunchTapTime;
    private float lastPunchActionTime;

    private Vector2 lastMoveDirection;
    private float lastMoveTime;

    private Coroutine singleMoveCoroutine;
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

    private void Awake()
    {

        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        kickAction = playerInput.actions["Kick"];
        punchAction = playerInput.actions["Punch"];
        healthBar.Initialize(health);

    }

    private void OnEnable()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveCancel;
        kickAction.performed += OnKick;
        punchAction.performed += OnPunch;
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCancel;
        kickAction.performed -= OnKick;
        punchAction.performed -= OnPunch;
    }

    private void FixedUpdate()
    {
        animator.SetInteger("weaponID", weaponType);

        if (PlayerPrefs.GetInt("WeaponIDPREF") == 1)
        {
            isChainsaw = false;
            weaponType = 1;
        }

        if (PlayerPrefs.GetInt("WeaponIDPREF") == 2)
        {
            isChainsaw = false;
            weaponType = 2;
        }
        if (PlayerPrefs.GetInt("WeaponIDPREF") == 3)
        {
            isChainsaw = true;
            weaponType = 2;
        }
        //////////////////////////////////////////////
        if (weaponType == 1)
        {
            axeWeaponhand.SetActive(false);
            CHainsawweaponhand.SetActive(false);
        }
        if (weaponType == 2)
        {
            if (isChainsaw)
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
        healthBar.UpdateHealth(health);
        if (!allowDash)
        {
            Invoke(nameof(resetDashcooldown), dashCoolDown);
        }
        FaceTarget();
        // Handle dashing first (highest priority)
        if (isDashing)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, dashDirection * moveSpeed * dashintennsity);
            dashTimer += Time.fixedDeltaTime;

            if (dashTimer >= dashDuration)
            {
                isDashing = false;
                dashTimer = 0f;
                isAttacking = false;
            }
            return;
        }

        if (!allowInput && (animator.GetInteger("hvmove") == 7 || animator.GetInteger("hvmove") == -7))
        {
            animator.SetInteger("hvmove", 0);
        }
        // 👈 ADD THIS to block normal movement

        // Normal movement logic
        Vector2 freshInput = moveAction.ReadValue<Vector2>();

        if (!isMovementLocked && !isAttacking && !isDashing)
        {
            if (freshInput != Vector2.zero)
            {
                int multiplier = Mathf.RoundToInt(Mathf.Clamp(freshInput.x, -1f, 1f));
                animator.SetInteger("hvmove", 7 * faceStyle * multiplier);
                Vector3 moveDir = new Vector3(0, 0, freshInput.x);
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
    public void resetDashcooldown()
    {
        CancelInvoke(nameof(resetDashcooldown));
        allowDash = true;
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
            faceStyle = 1;
        }
        else if (directionToTarget.z < 0)
        {
            faceStyle = -1;

            // Face backward (rotation = 180)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }


    // ---------------- MOVEMENT ----------------
    private void OnMove(InputAction.CallbackContext ctx)
    {
        // Allow dash input even when allowInput is false, but block regular movement
        Vector2 newInput = ctx.ReadValue<Vector2>();
        currentMoveInput = newInput;
        moveAxis = newInput;

        if (moveAxis != Vector2.zero)
        {
            if (singleMoveCoroutine != null)
            {
                StopCoroutine(singleMoveCoroutine);
                singleMoveCoroutine = null;
            }

            if (Time.time - lastMoveTime < doubleTapWindow && lastMoveDirection == moveAxis)
            {
                OnDoubleTapMove(moveAxis);  // Always allow dash, even during attacks
            }
            else if (allowInput)  // Only allow single tap moves when input is allowed
            {
                singleMoveCoroutine = StartCoroutine(SingleMoveRoutineDelayed(moveAxis));
            }

            lastMoveDirection = moveAxis;
            lastMoveTime = Time.time;
        }
    }

    private void OnMoveCancel(InputAction.CallbackContext ctx)
    {
        if (!allowInput) return; // 👈 BLOCK INPUT

        currentMoveInput = Vector2.zero;
        moveAxis = Vector2.zero;

        if (singleMoveCoroutine != null)
        {
            StopCoroutine(singleMoveCoroutine);
            singleMoveCoroutine = null;
        }

        moveConsumed = false;
    }

    private IEnumerator SingleMoveRoutineDelayed(Vector2 dir)
    {
        float start = Time.time;
        yield return new WaitForSeconds(singleTapDelay);

        if (!moveConsumed && Time.time - start < doubleTapWindow && lastMoveDirection == dir)
        {
            OnSingleTapMove(dir);
        }

        moveConsumed = false;
        singleMoveCoroutine = null;
    }

    private void OnSingleTapMove(Vector2 dir)
    {
        // Priority system: kick > jump > crouch
    }

    private void OnDoubleTapMove(Vector2 dir)
    {
        if ((dir.x > 0 && dir.y == 0) && allowDash)
        {
            allowDash = false;

            // Check if movement is locked (indicates we're in an attack animation)
            if (isMovementLocked || !allowInput)
            {
                // Start dash movement without triggering animation during attacks
                StartDash(1f, moveMovementLocks[2]);
            }
            else
            {
                // Normal dash with animation when not attacking
                TriggerCombo("hvmove", 3, moveMovementLocks[2], moveAnimResetDurations[2]);
                StartDash(1f, moveMovementLocks[2]);
            }
        }
        else if ((dir.x < 0 && dir.y == 0) && allowDash)
        {
            allowDash = false;

            // Check if movement is locked (indicates we're in an attack animation)
            if (isMovementLocked || !allowInput)
            {
                // Start dash movement without triggering animation during attacks
                StartDash(-1f, moveMovementLocks[3]);
            }
            else
            {
                // Normal dash with animation when not attacking
                TriggerCombo("hvmove", 4, moveMovementLocks[3], moveAnimResetDurations[3]);
                StartDash(-1f, moveMovementLocks[3]);
            }
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

    // ---------------- KICK ----------------
    private void OnKick(InputAction.CallbackContext ctx)
    {
        if (!allowInput) return; // 👈 BLOCK INPUT

        float now = Time.time;

        if (now - lastKickActionTime < inputCooldown && !(now - lastKickTapTime <= doubleTapWindow))
            return;

        if (now - lastKickTapTime <= doubleTapWindow)
        {
            if (moveAxis.x > 0 && moveAxis.y == 0) { TriggerAttack("kickID", 6, attackMovementLocks[5], attackAnimResetDurations[5]); }
            if (moveAxis.x < 0 && moveAxis.y < 0) { TriggerAttack("kickID", 8, attackMovementLocks[6], attackAnimResetDurations[6]); }
            if (moveAxis.x < 0 && moveAxis.y == 0) { TriggerAttack("kickID", 9, attackMovementLocks[7], attackAnimResetDurations[7]); }

            lastKickActionTime = now;
            lastKickTapTime = 0f;
            moveConsumed = true;
            return;
        }

        if (moveAxis.x > 0 && moveAxis.y == 0) { TriggerAttack("kickID", 1, attackMovementLocks[0], attackAnimResetDurations[0]); moveConsumed = true; }
        if (moveAxis.x > 0 && moveAxis.y < 0) { TriggerAttack("kickID", 2, attackMovementLocks[1], attackAnimResetDurations[1]); moveConsumed = true; }
        if (moveAxis.x == 0 && moveAxis.y < 0) { TriggerAttack("kickID", 3, attackMovementLocks[2], attackAnimResetDurations[2]); moveConsumed = true; }
        if ((moveAxis.x < 0) && (moveAxis.y == 0 || moveAxis.y < 0)) { TriggerAttack("kickID", 4, attackMovementLocks[3], attackAnimResetDurations[3]); moveConsumed = true; }
        if (moveAxis.y > 0) { TriggerAttack("kickID", 5, attackMovementLocks[4], attackAnimResetDurations[4]); moveConsumed = true; }

        lastKickActionTime = now;
        lastKickTapTime = now;
    }

    // ---------------- PUNCH ----------------
    private void OnPunch(InputAction.CallbackContext ctx)
    {
        if (!allowInput) return; // 👈 BLOCK INPUT

        float now = Time.time;

        if (now - lastPunchActionTime < inputCooldown && !(now - lastPunchTapTime <= doubleTapWindow))
            return;

        if (now - lastPunchTapTime <= doubleTapWindow)
        {

            if (moveAxis.x > 0 && moveAxis.y == 0)
            {
                TriggerAttack("punchID", 1, attackMovementLocks[8], attackAnimResetDurations[8]);
                if (weaponType == 1)
                {
                    StartCoroutine(PlayerPublicMove(.5f, 4, faceStyle));
                }
                if (weaponType == 2)
                {
                    StartCoroutine(PlayerPublicMove(1, 3, faceStyle));
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

                StartCoroutine(PlayerPublicMove(.5f, 4, faceStyle));
            }
            if (weaponType == 2)
            {
                // StartCoroutine(PlayerPublicMove(1, 7, faceStyle));
            }
        }
        if (moveAxis.x < 0 && moveAxis.y == 0)
        {
            if (weaponType == 1)
            {
                //StartCoroutine(PlayerPublicMove(.5f, 4, faceStyle));
            }
            if (weaponType == 2)
            {
                TriggerAttack("punchID", 3, attackMovementLocks[10], attackAnimResetDurations[10]);
                //StartCoroutine(PlayerPublicMove(1, 7, faceStyle));
            }
        }

        lastPunchActionTime = now;
        lastPunchTapTime = now;
        moveConsumed = true;
    }

    IEnumerator PlayerPublicMove(float lifetime, float speed, int zAxis)
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

    // Optimized parameter management - no more coroutines for resets!
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

    public enum HitPart { Head, Body, Limb, LegLimb }

    public void ApplyHit(HitPart part, bool sweep)
    {
        float damage = defaultBaseDamage;
        float bonusDuration = 1f;
        Vector3 hitPosition = Vector3.zero; // 👈 stores where we got hit

        // stop rb instantly on hit
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        switch (part)
        {
            case HitPart.Head:
                damage = headBaseDamage;
                hitPosition = headTransform.position; // 👈 world coordinates of head
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
                Debug.Log("Head hit at " + hitPosition);
                break;

            case HitPart.Body:
                damage = bodyBaseDamage;
                hitPosition = bodyTransform.position; // 👈 world coordinates of body
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
                StartCoroutine(PlayerPublicMove(.5f, 5, -faceStyle));
                invinsible = true;
                Debug.Log("Body hit at " + hitPosition);
                break;

            case HitPart.Limb:
                damage = limbBaseDamage;
                hitPosition = limbTransform.position; // 👈 use correct limb reference
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
                    StartCoroutine(PlayerPublicMove(.5f, 5, -faceStyle));
                }
                invinsible = true;
                Debug.Log("Leg hit at " + hitPosition);
                break;
            case HitPart.LegLimb:
                damage = limbBaseDamage;
                hitPosition = legLimbTransform.position; // 👈 use correct limb reference
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
                    StartCoroutine(PlayerPublicMove(.5f, 5, -faceStyle));
                }
                invinsible = true;
                Debug.Log("Limb hit at " + hitPosition);
                break;

            default:
                Debug.Log("Other hit!");
                break;
        }

        allowInput = false;
        StartCoroutine(InputSwitch(bonusDuration));
        TakeDamage(damage);

        // 👇 Call helper function with the hit position
        ReportHitPosition(part, hitPosition);
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

            // ✅ Use RectTransform anchoredPosition, not localPosition
            RectTransform rect = effectHEADTEXT.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(143.6f, -80.6f);

            Destroy(effectHEADTEXT, 1f);
        }


        else
        {
            effect = Instantiate(POWhitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 0.6f);

        }
    }



    IEnumerator InputSwitch(float delay)
    {
        Hbonus = false;
        Bbonus = false;
        Lbonus = false;
        yield return new WaitForSeconds(delay);
        allowInput = true;
        invinsible = false;
    }
    // Enhanced animation event handlers for better timing control
    public void OnAttackStart()
    {
        // Called at the beginning of attack animation
        isMovementLocked = true;
        CancelInvoke(nameof(ResetAllAttackParams)); // Cancel any pending resets
    }

    public void OnAttackEnd()
    {
        // Called at the end of attack animation
        isMovementLocked = false;
        isAttacking = false;
        // Immediate reset when animation ends (more responsive)
        ResetAllAttackParams();
    }

    public void OnMoveStart()
    {
        // Called when movement animation starts
        isMovementLocked = true;
        CancelInvoke(nameof(ResetAllMoveParams));
    }

    public void OnMoveEnd()
    {
        // Called when movement animation ends
        isMovementLocked = false;
        ResetAllMoveParams();
    }

    public void OnHitboxActive()
    {
        // Called when hitbox should be active
        // Enable attack colliders here
        Debug.Log("Hitbox Active");
    }

    public void OnHitboxInactive()
    {
        // Called when hitbox should be disabled
        // Disable attack colliders here
        Debug.Log("Hitbox Inactive");
    }


    private void TakeDamage(float amount)
    {
        CombatInputHandler rootHandler = GetComponentInParent<CombatInputHandler>();
        if (rootHandler != null && rootHandler != this)
        {
            rootHandler.TakeDamage(amount);
            return;
        }

        health -= amount;
        health = Mathf.Clamp(health, 0, 1000);
        Debug.Log("Health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
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
        Debug.Log("Player is dead!");
        enabled = false;
    }
}