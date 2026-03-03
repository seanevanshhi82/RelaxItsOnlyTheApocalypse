using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))] // Optional: needs a collider for proximity check
public class Door : MonoBehaviour
{
    [Header("Door State")]
    [Tooltip("Is the door currently closed? (true = closed, false = open)")]
    public bool isClosed = true;

    [Tooltip("Does the door open outward (swing) instead of sliding? (future use)")]
    public bool OpenOutward = false;

    [Tooltip("Is the door locked? (future use - prevents opening)")]
    public bool isLocked = false;

    [Tooltip("Is the door barricaded? (future use - prevents opening)")]
    public bool isBarricaded = false;

    [Tooltip("Is this a sliding door? (true = slides on Z axis, false = future swing/rotate)")]
    public bool SlidingDoor = true;

    [Tooltip("Slide direction for closing: true = left (positive Z), false = right (negative Z)")]
    public bool CloseLeft = true;

    [Tooltip("Is this a garage-style door? (future use)")]
    public bool GarageDoor = false;

    [Tooltip("Does this door require a key? (future use)")]
    public bool hasKey = false;

    [Header("Movement Settings")]
    [Tooltip("Maximum distance player must be from door center to interact (meters)")]
    public float OpenDistance = 2.5f;

    [Tooltip("How far the door slides open/close (units)")]
    public float slideAmount = 1.5f;

    [Tooltip("How fast the door slides (units per second)")]
    public float slideSpeed = 2f;

    [Header("Input & References")]
    [Tooltip("Reference to the player's Transform (drag player object here or auto-find)")]
    public Transform playerTransform;

    private Vector3 initialPosition;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private float moveProgress = 0f;

    void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition;

        // Optional: auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("Door: Auto-found Player transform");
            }
            else
            {
                Debug.LogWarning("Door: No Player found with tag 'Player' — assign playerTransform manually");
            }
        }
    }

    void Update()
    {
        if (isMoving)
        {
            moveProgress += Time.deltaTime * slideSpeed;
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveProgress);

            // Stop moving when reached target
            if (moveProgress >= 1f)
            {
                transform.position = targetPosition;
                isMoving = false;
                moveProgress = 0f;
            }
        }
    }

    // Called from PlayerInput / Interact action (or you can call it manually)
    public void Interact()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > OpenDistance) return; // Too far

        // Toggle door state
        if (isClosed)
        {
            // Trying to open
            if (isLocked || isBarricaded)
            {
                Debug.Log("Door is locked or barricaded — cannot open");
                return;
            }

            OpenDoor();
        }
        else
        {
            // Close the door
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        isClosed = false;

        if (SlidingDoor)
        {
            float direction = CloseLeft ? -1f : 1f; // Open opposite of close direction
            targetPosition = initialPosition + new Vector3(0f, 0f, direction * slideAmount);
            isMoving = true;
            moveProgress = 0f;
            Debug.Log("Sliding door opening " + (CloseLeft ? "left" : "right"));
        }
        else
        {
            // Future: swing outward or other animation
            Debug.Log("Door opening (swing - future implementation)");
        }
    }

    private void CloseDoor()
    {
        isClosed = true;

        if (SlidingDoor)
        {
            float direction = CloseLeft ? 1f : -1f; // Close back to original
            targetPosition = initialPosition + new Vector3(0f, 0f, direction * slideAmount);
            isMoving = true;
            moveProgress = 0f;
            Debug.Log("Sliding door closing");
        }
        else
        {
            // Future: swing back
            Debug.Log("Door closing (swing - future implementation)");
        }
    }

    // Optional: Draw interaction range in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, OpenDistance);
    }
}