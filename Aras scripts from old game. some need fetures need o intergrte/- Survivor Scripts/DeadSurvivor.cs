using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DeadSurvivor : MonoBehaviour
{
    [Header("Dead Pose Settings")]
    [Tooltip("Randomly selects 0-3 at start to play one of 4 death poses")]
    [SerializeField] private int deadPose;

    [Header("Body Health Pool")]
    [Tooltip("Total health the body has for zombies to feed on (e.g., 2× zombie max)")]
    public float maxBodyHealth = 240f;

    [Tooltip("How much health is drained per tick per zombie feeding")]
    public float drainPerFeeder = 5f;

    [Tooltip("Time between drain ticks (seconds)")]
    public float drainRate = 2f;

    [Header("Turning into Zombie")]
    [Tooltip("Can this body turn into a zombie after being consumed?")]
    public bool canTurn = false;

    [Tooltip("Time (seconds) after being fully consumed before turning (if canTurn = true)")]
    public float turnTimer = 30f;

    [Header("Consumed State")]
    [Tooltip("Optional skeleton prefab to swap to when fully consumed")]
    public GameObject skeletonPrefab;

    private Animator anim;
    private float currentBodyHealth;
    private float drainTimer = 0f;
    private int feederCount = 0; // Number of zombies currently feeding
    private float turnCountdown = 0f;
    private bool isTurning = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("DeadSurvivor: No Animator found on " + gameObject.name);
            enabled = false;
            return;
        }

        // Safety: make sure zombie scripts start disabled
        if (TryGetComponent<BasicInfectedHealth>(out var health))
            health.enabled = false;
        if (TryGetComponent<BasicInfectedAI>(out var ai))
            ai.enabled = false;
    }

    void Start()
    {
        // Random pose
        deadPose = Random.Range(0, 4); // 0-3 inclusive
        anim.SetInteger("DeadPose", deadPose);
        anim.Play("DeadPose" + deadPose, 0, 0f);

        currentBodyHealth = maxBodyHealth;
        Debug.Log(gameObject.name + " spawned in DeadPose " + deadPose + " with " + currentBodyHealth + " body health");
    }

    void Update()
    {
        if (currentBodyHealth <= 0f)
        {
            if (feederCount <= 0 && !isTurning)
            {
                OnConsumed();
            }

            // Turning countdown
            if (canTurn && !isTurning)
            {
                turnCountdown += Time.deltaTime;
                if (turnCountdown >= turnTimer)
                {
                    TurnIntoZombie();
                }
            }
            return;
        }

        if (feederCount > 0)
        {
            drainTimer += Time.deltaTime;
            if (drainTimer >= drainRate)
            {
                float drainAmount = drainPerFeeder * feederCount;
                currentBodyHealth -= drainAmount;
                currentBodyHealth = Mathf.Max(currentBodyHealth, 0f);
                drainTimer = 0f;
                Debug.Log(gameObject.name + " drained " + drainAmount + " (by " + feederCount + " zombies) — remaining: " + currentBodyHealth);
            }
        }
    }

    public void AddFeeder()
    {
        feederCount++;
        Debug.Log("Feeder added to " + gameObject.name + " — total: " + feederCount);
    }

    public void RemoveFeeder()
    {
        feederCount = Mathf.Max(feederCount - 1, 0);
        Debug.Log("Feeder removed from " + gameObject.name + " — total: " + feederCount);
    }

    private void OnConsumed()
    {
        Debug.Log(gameObject.name + " fully consumed!");

        if (skeletonPrefab != null)
        {
            Instantiate(skeletonPrefab, transform.position, transform.rotation);
        }

        if (canTurn)
        {
            turnCountdown = 0f;
            Debug.Log(gameObject.name + " can turn — countdown started (" + turnTimer + "s)");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void TurnIntoZombie()
    {
        isTurning = true;
        Debug.Log(gameObject.name + " turning into zombie!");

        // Change root tag and layer
        gameObject.tag = "Infected";
        gameObject.layer = LayerMask.NameToLayer("Infected");

        // Set animator parameter
        anim.SetBool("Turned", true);
        Debug.Log("Animator parameter 'Turned' set to true");

        // Enable zombie scripts (they must already exist on this object, disabled by default)
        if (TryGetComponent<BasicInfectedHealth>(out var health))
        {
            health.enabled = true;
            Debug.Log("BasicInfectedHealth enabled");
        }
        else
        {
            Debug.LogWarning("BasicInfectedHealth component not found on " + gameObject.name);
        }

        if (TryGetComponent<BasicInfectedAI>(out var ai))
        {
            ai.enabled = true;
            Debug.Log("BasicInfectedAI enabled");

            // Configure AI layers
            ai.targetLayer = LayerMask.GetMask("Player", "Ally", "Camp Resident", "Survivor", "Enemy");
            ai.deadLayer = LayerMask.GetMask("Dead");
            ai.obstacleLayer = LayerMask.GetMask("Obstacle", "Door", "Cover");

            // Auto-assign hand colliders from children
            MeleeDamageCollider[] handColliders = GetComponentsInChildren<MeleeDamageCollider>();
            if (handColliders.Length >= 2)
            {
                ai.leftHandCollider = handColliders[0].GetComponent<Collider>();
                ai.rightHandCollider = handColliders[1].GetComponent<Collider>();
                Debug.Log("Hand colliders auto-assigned to BasicInfectedAI");
            }
            else
            {
                Debug.LogWarning("Not enough MeleeDamageCollider children found — hand colliders not auto-assigned");
            }
        }
        else
        {
            Debug.LogWarning("BasicInfectedAI component not found on " + gameObject.name);
        }

        // Change any child objects on "Dead" layer to "Infected"
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var child in allChildren)
        {
            if (child.gameObject.layer == LayerMask.NameToLayer("Dead"))
            {
                child.gameObject.layer = LayerMask.NameToLayer("Infected");
                Debug.Log("Changed child layer from Dead to Infected: " + child.name);
            }
        }

        // Disable this script
        enabled = false;
        Debug.Log("DeadSurvivor script disabled — zombie transformation complete");
    }
}