using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class WeaponHandler : MonoBehaviour, ActionInputs.IGamplayActions
{
    [Header("Weapons")]
    [Tooltip("Drag your melee weapon GameObject here (will be activated/deactivated)")]
    public GameObject meleeWeapon;

    [Tooltip("Drag your ranged weapon GameObject here (gun/bow model)")]
    public GameObject rangedWeapon;

    [Header("Ranged/Thrown Settings")]
    [Tooltip("The empty Transform at the gun muzzle or bow hand (for spawn position/rotation)")]
    public Transform barrel;

    [Tooltip("Your projectile prefab with Projectile.cs attached (arrow/bullet)")]
    public GameObject projectilePrefab;

    [Tooltip("Muzzle flash particle prefab (auto-destroy after 0.1s)")]
    public GameObject muzzleFlashPrefab;

    [Tooltip("Sound effect for shooting/throwing")]
    public AudioClip shootSound;

    [Tooltip("Minimum time between shots/throws (fire rate in seconds)")]
    public float timeBetweenShoots = 0.2f;

    [Header("Arrow/Thrown Visuals")]
    [Tooltip("The arrow model in the hand (for visibility toggle - disappears on release, reappears for next shot)")]
    public GameObject arrowModel; // Drag the arrow mesh here

    [Header("Unarmed Colliders")]
    [Tooltip("Unarmed left hand/weapon collider (optional)")]
    public Collider unarmedLeftCollider;

    [Tooltip("Unarmed right hand/weapon collider (optional)")]
    public Collider unarmedRightCollider;

    [Header("Melee Colliders")]
    [Tooltip("Melee left hand/weapon collider (optional)")]
    public Collider meleeLeftCollider;

    [Tooltip("Melee right hand/weapon collider (optional)")]
    public Collider meleeRightCollider;

    [Header("Stamina Costs")]
    [Tooltip("Stamina cost for Jump")]
    public float jumpStaminaCost = 10f;

    [Tooltip("Stamina cost for ChargedAttack")]
    public float chargedAttackStaminaCost = 15f;

    [Tooltip("Stamina cost for StrongCombo")]
    public float strongComboStaminaCost = 15f;

    [Tooltip("Stamina cost for QuickCombo")]
    public float quickComboStaminaCost = 10f;

    [Header("Cooldowns (seconds) - for testing")]
    [Tooltip("Cooldown for Jump")]
    public float jumpCooldown = 1f;

    [Tooltip("Cooldown for ChargedAttack")]
    public float chargedAttackCooldown = 2f;

    [Tooltip("Cooldown for StrongCombo")]
    public float strongComboCooldown = 3f;

    [Tooltip("Cooldown for QuickCombo")]
    public float quickComboCooldown = 2f;

    [Header("Other")]
    [Tooltip("Placeholder for future alert/agro system")]
    public bool alert;

    [Header("Targeting (optional - drag from your Targeting script)")]
    [Tooltip("If assigned, projectiles aim at this target. Otherwise raycasts forward.")]
    public Transform currentTarget;

    [Header("Startup Mode")]
    [Tooltip("Which mode to start in (Unarmed, Melee, Ranged, Thrown)")]
    [SerializeField] private StartupMode startupMode = StartupMode.Unarmed;

    private enum StartupMode { Unarmed, Melee, Ranged, Thrown }

    public Animator anim;
    private AudioSource audioSrc;
    private int meleeLayerIndex = -1;
    private int rangedLayerIndex = -1;
    private int thrownLayerIndex = -1; // New for thrown mode
    private float lastShootTime;

    // Cooldown timers
    private float lastJumpTime;
    private float lastChargedAttackTime;
    private float lastStrongComboTime;
    private float lastQuickComboTime;

    private ActionInputs inputActions;

    void Awake()
    {
        anim = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();

        meleeLayerIndex = anim.GetLayerIndex("Melee Layer");
        rangedLayerIndex = anim.GetLayerIndex("Ranged Layer");
        thrownLayerIndex = anim.GetLayerIndex("Thrown Layer");

        if (meleeLayerIndex == -1) Debug.LogWarning("WeaponHandler: 'Melee Layer' not found in Animator!", this);
        if (rangedLayerIndex == -1) Debug.LogWarning("WeaponHandler: 'Ranged Layer' not found in Animator!", this);
        if (thrownLayerIndex == -1) Debug.LogWarning("WeaponHandler: 'Thrown Layer' not found in Animator!", this);

        inputActions = new ActionInputs();
        inputActions.Gamplay.SetCallbacks(this);

        // Safety: force trigger mode on colliders
        if (unarmedLeftCollider != null) unarmedLeftCollider.isTrigger = true;
        if (unarmedRightCollider != null) unarmedRightCollider.isTrigger = true;
        if (meleeLeftCollider != null) meleeLeftCollider.isTrigger = true;
        if (meleeRightCollider != null) meleeRightCollider.isTrigger = true;
    }

    void Start()
    {
        switch (startupMode)
        {
            case StartupMode.Unarmed:
                SwitchToUnarmed();
                break;
            case StartupMode.Melee:
                SwitchToMelee();
                break;
            case StartupMode.Ranged:
                SwitchToRanged();
                break;
            case StartupMode.Thrown:
                SwitchToThrown();
                break;
        }
    }

    void OnEnable()
    {
        inputActions.Gamplay.Enable();
    }

    void OnDisable()
    {
        inputActions.Gamplay.Disable();
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Input Action Callbacks – FULL interface implementation
    // ──────────────────────────────────────────────────────────────────────────────

    public void OnSwitchToUnarmed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SwitchToUnarmed();
        }
    }

    public void OnSwitchToMelee(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SwitchToMelee();
        }
    }

    public void OnSwitchToRange(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SwitchToRanged();
        }
    }

    public void OnQuickAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnQuickAttackPerformed();
        }
    }

    public void OnStrongAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnStrongAttackPerformed();
        }
    }

    public void OnQuickCombo(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            int mode = anim.GetInteger("CombatMode");
            if (mode == 0 || mode == 1)
            {
                if (Time.time >= lastQuickComboTime + quickComboCooldown)
                {
                    if (UseStamina(quickComboStaminaCost))
                    {
                        anim.SetTrigger("QuickCombo");
                        Debug.Log("QuickCombo triggered (CombatMode " + mode + ")");
                        lastQuickComboTime = Time.time;
                    }
                    else
                    {
                        Debug.Log("QuickCombo failed: not enough stamina");
                    }
                }
                else
                {
                    Debug.Log("QuickCombo on cooldown");
                }
            }
        }
    }

    public void OnStrongCombo(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            int mode = anim.GetInteger("CombatMode");
            if (mode == 0 || mode == 1)
            {
                if (Time.time >= lastStrongComboTime + strongComboCooldown)
                {
                    if (UseStamina(strongComboStaminaCost))
                    {
                        anim.SetTrigger("StrongCombo");
                        Debug.Log("StrongCombo triggered (CombatMode " + mode + ")");
                        lastStrongComboTime = Time.time;
                    }
                    else
                    {
                        Debug.Log("StrongCombo failed: not enough stamina");
                    }
                }
                else
                {
                    Debug.Log("StrongCombo on cooldown");
                }
            }
        }
    }

    public void OnReloadPushAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            int mode = anim.GetInteger("CombatMode");
            if (mode == 0 || mode == 1)
            {
                anim.SetTrigger("ReloadPushAttack");
                Debug.Log("ReloadPushAttack triggered (CombatMode " + mode + ")");
                ApplyKnockdownToTarget();
            }
        }
    }

    public void OnChargedAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            int mode = anim.GetInteger("CombatMode");
            if (mode == 0 || mode == 1)
            {
                if (Time.time >= lastChargedAttackTime + chargedAttackCooldown)
                {
                    if (UseStamina(chargedAttackStaminaCost))
                    {
                        anim.SetTrigger("ChargedAttack");
                        Debug.Log("ChargedAttack triggered (CombatMode " + mode + ")");
                        lastChargedAttackTime = Time.time;
                    }
                    else
                    {
                        Debug.Log("ChargedAttack failed: not enough stamina");
                    }
                }
                else
                {
                    Debug.Log("ChargedAttack on cooldown");
                }
            }
        }
    }

    // Stub remaining methods (prevents MissingMethod spam)
    public void OnJump(InputAction.CallbackContext context) { }
    public void OnPass(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnRun(InputAction.CallbackContext context) { }
    public void OnDodge(InputAction.CallbackContext context) { }
    public void OnBlock(InputAction.CallbackContext context) { }
    public void OnReload(InputAction.CallbackContext context) { }
    public void OnSwitchLeader(InputAction.CallbackContext context) { }
    public void OnEquipment(InputAction.CallbackContext context) { }
    public void OnZoomToggle(InputAction.CallbackContext context) { }
    public void OnProne(InputAction.CallbackContext context) { }
    public void OnMove(InputAction.CallbackContext context) { }

    // ──────────────────────────────────────────────────────────────────────────────
    // Missing Methods Added (these were called but not defined)
    // ──────────────────────────────────────────────────────────────────────────────

    private void OnQuickAttackPerformed()
    {
        if (anim == null)
        {
            Debug.LogError("Animator missing on " + gameObject.name + " — cannot set trigger!");
            return;
        }

        Debug.Log("QuickAttack performed → setting 'QuickAttack' trigger");
        anim.SetTrigger("QuickAttack");
    }

    private void OnStrongAttackPerformed()
    {
        if (anim == null)
        {
            Debug.LogError("Animator missing on " + gameObject.name + " — cannot set trigger!");
            return;
        }

        Debug.Log("StrongAttack performed → setting 'StrongAttack' trigger");
        anim.SetTrigger("StrongAttack");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Stamina Check & Subtract (now safe using public method)
    // ──────────────────────────────────────────────────────────────────────────────

    private bool UseStamina(float cost)
    {
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("WeaponHandler: PlayerHealth component missing on " + gameObject.name);
            return false;
        }

        if (playerHealth.CurrentStamina >= cost)
        {
            playerHealth.CurrentStamina -= cost;
            Debug.Log("Stamina used: " + cost + " — remaining: " + playerHealth.CurrentStamina);
            return true;
        }
        else
        {
            Debug.Log("Not enough stamina for action");
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // ReloadPushAttack Hit Detection (raycast forward to apply isKnockdown)
    // ──────────────────────────────────────────────────────────────────────────────

    private void ApplyKnockdownToTarget()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 5f))
        {
            Animator targetAnim = hit.collider.GetComponent<Animator>();
            if (targetAnim != null)
            {
                targetAnim.SetTrigger("isKnockdown");
                Debug.Log("ReloadPushAttack hit target — triggered isKnockdown");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Unarmed & Melee Collider Methods (called from animation events)
    // ──────────────────────────────────────────────────────────────────────────────

    public void EnableUnarmedColliders()
    {
        if (unarmedLeftCollider != null) unarmedLeftCollider.enabled = true;
        if (unarmedRightCollider != null) unarmedRightCollider.enabled = true;
        Debug.Log("Unarmed colliders ENABLED");
    }

    public void DisableUnarmedColliders()
    {
        if (unarmedLeftCollider != null) unarmedLeftCollider.enabled = false;
        if (unarmedRightCollider != null) unarmedRightCollider.enabled = false;
        Debug.Log("Unarmed colliders DISABLED");
    }

    public void EnableMeleeColliders()
    {
        if (meleeLeftCollider != null) meleeLeftCollider.enabled = true;
        if (meleeRightCollider != null) meleeRightCollider.enabled = true;
        Debug.Log("Melee colliders ENABLED");
    }

    public void DisableMeleeColliders()
    {
        if (meleeLeftCollider != null) meleeLeftCollider.enabled = false;
        if (meleeRightCollider != null) meleeRightCollider.enabled = false;
        Debug.Log("Melee colliders DISABLED");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Switch Methods
    // ──────────────────────────────────────────────────────────────────────────────

    private void SwitchToUnarmed()
    {
        if (meleeWeapon) meleeWeapon.SetActive(false);
        if (rangedWeapon) rangedWeapon.SetActive(false);

        anim.SetInteger("CombatMode", 0);

        if (meleeLayerIndex >= 0) anim.SetLayerWeight(meleeLayerIndex, 0f);
        if (rangedLayerIndex >= 0) anim.SetLayerWeight(rangedLayerIndex, 0f);
    }

    private void SwitchToMelee()
    {
        if (meleeWeapon) meleeWeapon.SetActive(true);
        if (rangedWeapon) rangedWeapon.SetActive(false);

        anim.SetInteger("CombatMode", 1);

        if (meleeLayerIndex >= 0) anim.SetLayerWeight(meleeLayerIndex, 1f);
        if (rangedLayerIndex >= 0) anim.SetLayerWeight(rangedLayerIndex, 0f);
    }

    private void SwitchToRanged()
    {
        if (meleeWeapon) meleeWeapon.SetActive(false);
        if (rangedWeapon) rangedWeapon.SetActive(true);

        anim.SetInteger("CombatMode", 2);

        if (rangedLayerIndex >= 0) anim.SetLayerWeight(rangedLayerIndex, 1f);
        if (meleeLayerIndex >= 0) anim.SetLayerWeight(meleeLayerIndex, 0f);
    }

    // NEW: Added this method to fix the "SwitchToThrown does not exist" error
    private void SwitchToThrown()
    {
        if (meleeWeapon) meleeWeapon.SetActive(false);
        if (rangedWeapon) rangedWeapon.SetActive(true);

        anim.SetInteger("CombatMode", 3);

        if (thrownLayerIndex >= 0) anim.SetLayerWeight(thrownLayerIndex, 1f);
        if (meleeLayerIndex >= 0) anim.SetLayerWeight(meleeLayerIndex, 0f);
        if (rangedLayerIndex >= 0) anim.SetLayerWeight(rangedLayerIndex, 0f);

        Debug.Log("Switched to Thrown mode (CombatMode = 3)");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Ranged Shooting Logic (unchanged)
    // ──────────────────────────────────────────────────────────────────────────────

    private void Shoot()
    {
        lastShootTime = Time.time;
        Debug.Log("Executing Shoot! (muzzle flash + sound + projectile)");

        if (muzzleFlashPrefab && barrel)
        {
            var flash = Instantiate(muzzleFlashPrefab, barrel.position, barrel.rotation, barrel);
            Destroy(flash, 0.1f);
        }

        if (shootSound && audioSrc)
        {
            audioSrc.PlayOneShot(shootSound);
        }

        if (projectilePrefab && barrel)
        {
            Vector3 dir = GetShootDirection();
            var projObj = Instantiate(projectilePrefab, barrel.position, Quaternion.LookRotation(dir));
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Launch(gameObject, dir);
            }
        }
    }

    private Vector3 GetShootDirection()
    {
        if (currentTarget != null)
        {
            return (currentTarget.position + Vector3.up * 0.5f - barrel.position).normalized;
        }

        Ray ray = new Ray(barrel.position, barrel.forward);
        int ignoreMask = LayerMask.GetMask("Player", "Projectile");
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~ignoreMask))
        {
            return (hit.point - barrel.position).normalized;
        }

        return barrel.forward;
    }
}