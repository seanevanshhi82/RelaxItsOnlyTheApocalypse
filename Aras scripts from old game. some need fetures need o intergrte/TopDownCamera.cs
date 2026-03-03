using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Auto-finds object tagged 'Player' — updates if it changes")]
    public GameObject player;                     // Optional drag override

    [Header("Follow Settings")]
    [Tooltip("How fast the camera follows the player (higher = snappier)")]
    public float followSpeed = 10f;

    [Header("Zoom Levels")]
    [Tooltip("Close zoom (distance above player)")]
    public float closeZoomDistance = 8f;
    [Tooltip("Far zoom (distance above player)")]
    public float farZoomDistance = 15f;

    [Header("Fixed Rotation")]
    public Vector3 fixedRotation = new Vector3(90f, 0f, 0f);  // Straight down

    // Internal
    private Camera cam;
    private InputAction zoomToggleAction;
    private bool isCloseZoom = true;
    private float currentZoomDistance;
    private Transform targetTransform;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("TopDownCamera needs a Camera component!");
            enabled = false;
            return;
        }

        currentZoomDistance = closeZoomDistance;

        // Zoom toggle: Z key or gamepad Start button
        zoomToggleAction = new InputAction("ZoomToggle", InputActionType.Button);
        zoomToggleAction.AddBinding("<Keyboard>/z");
        zoomToggleAction.AddBinding("<Gamepad>/start");
        zoomToggleAction.performed += ctx => ToggleZoom();

        FindPlayer();
    }

    void OnEnable()
    {
        zoomToggleAction.Enable();
    }

    void OnDisable()
    {
        zoomToggleAction.Disable();
    }

    void LateUpdate()
    {
        if (player == null || targetTransform == null)
        {
            FindPlayer();
            return;
        }

        // Desired position: directly above player at current zoom distance
        Vector3 targetPos = targetTransform.position + Vector3.up * currentZoomDistance;

        // Smoothly move to target position
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        // Enforce minimum Y (never drop below zoom distance)
        float minY = targetTransform.position.y + currentZoomDistance;
        if (transform.position.y < minY)
        {
            transform.position = new Vector3(
                transform.position.x,
                minY,
                transform.position.z
            );
        }

        // Fixed top-down rotation
        transform.rotation = Quaternion.Euler(fixedRotation);
    }

    private void FindPlayer()
    {
        if (player != null && targetTransform != null) return;

        GameObject found = GameObject.FindWithTag("Player");
        if (found != null)
        {
            player = found;
            targetTransform = found.transform;
            Debug.Log("TopDownCamera: Locked onto Player!");
        }
        else if (player != null)
        {
            targetTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("TopDownCamera: No 'Player' tagged object found. Tag your player or drag it manually.");
        }
    }

    private void ToggleZoom()
    {
        isCloseZoom = !isCloseZoom;
        currentZoomDistance = isCloseZoom ? closeZoomDistance : farZoomDistance;
        Debug.Log($"Camera zoom: {(isCloseZoom ? "Close" : "Far")} ({currentZoomDistance})");
    }
}