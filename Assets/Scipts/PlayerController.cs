using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// CS464 Assignment 02: Lectures 04 and 05.
// Assignment-only controller; no level-specific gameplay systems.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Part A - Movement and Jump")]
    [SerializeField, Min(0f)] private float speed = 5f;           // m/s
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.8f;   // m

    [Header("Part A - CheckSphere Ground Test")]
    [SerializeField] private Transform feet;
    [SerializeField, Min(0.01f)] private float checkRadius = 0.19f;
    [SerializeField] private LayerMask groundMask;

    [Header("Part B - Fair Jumping")]
    [SerializeField, Min(0f)] private float coyoteTime = 0.10f;
    [SerializeField, Min(0f)] private float jumpBuffer = 0.12f;
    [SerializeField, Min(1f)] private float fallMultiplier = 2.2f;

    [Header("Part B - ONE Juice Effect: Landing Squash")]
    [SerializeField] private Transform body;                      // mesh child, no collider
    [SerializeField, Min(0.01f)] private float squashDuration = 0.18f;
    [SerializeField, Range(0.1f, 1f)] private float squashY = 0.70f;

    private Rigidbody rb;
    private Transform cam;
    private InputAction moveAction;
    private InputAction jumpAction;

    private Vector3 move;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpQueued;
    private float lastGroundedTime = -1000f;
    private float lastPressedTime = -1000f;
    private float airborneTime;
    private Vector3 bodyRestScale;
    private Coroutine squashRoutine;

    public Rigidbody PlayerRigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (Camera.main != null)
            cam = Camera.main.transform;

        if (body != null)
            bodyRestScale = body.localScale;

        // Lecture 04's Input System alternative: template-wide Move / Jump actions.
        // Configure Project Settings > Input System Package > Input Actions.
        InputActionAsset actions = InputSystem.actions;
        if (actions == null)
        {
            Debug.LogError("Assign InputSystem_Actions as the project-wide Input Actions asset.", this);
            return;
        }

        moveAction = actions.FindAction("Move", false);
        jumpAction = actions.FindAction("Jump", false);
        if (moveAction == null || jumpAction == null)
            Debug.LogError("Move and Jump actions were not found in InputSystem_Actions.", this);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        if (squashRoutine != null)
        {
            StopCoroutine(squashRoutine);
            squashRoutine = null;
        }
        if (body != null)
            body.localScale = bodyRestScale;
    }

    private void Update()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        // ALL input in Update. Never read a one-frame button press in FixedUpdate.
        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        input = Vector2.ClampMagnitude(input, 1f);

        // Lecture 05: move relative to the current camera orientation.
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        move = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

        wasGrounded = isGrounded;
        isGrounded = feet != null && Physics.CheckSphere(
            feet.position, checkRadius, groundMask, QueryTriggerInteraction.Ignore);

        // One squash per real landing after > 0.1 s airborne.
        if (!isGrounded)
        {
            airborneTime += Time.deltaTime;
        }
        else
        {
            if (!wasGrounded && airborneTime > 0.10f && rb.linearVelocity.y <= 0f)
                PlayLandingSquash();
            airborneTime = 0f;
        }

        // Do not renew ground time on takeoff or while a jump is queued.
        if (isGrounded && !jumpQueued && rb.linearVelocity.y <= 0f)
            lastGroundedTime = Time.time;

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
            lastPressedTime = Time.time;

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastPressedTime <= jumpBuffer;
        if (!jumpQueued && canJump && wantsJump)
        {
            jumpQueued = true;
            lastGroundedTime = -1000f; // Consume coyote window.
            lastPressedTime = -1000f;  // Consume buffer window.
        }
    }

    private void FixedUpdate()
    {
        // A velocity is already in metres per second: no * Time.deltaTime here.
        Vector3 current = rb.linearVelocity;
        rb.linearVelocity = new Vector3(
            move.x * speed, current.y, move.z * speed);

        if (jumpQueued)
        {
            jumpQueued = false;
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, 0f, v.z); // Full-height late jump.

            // Lecture 05: v0 = sqrt(2gh); impulse = mass * v0.
            float g = -Physics.gravity.y;
            float launchSpeed = Mathf.Sqrt(2f * g * jumpHeight);
            rb.AddForce(Vector3.up * rb.mass * launchSpeed, ForceMode.Impulse);
        }

        // Extra gravity only while airborne and falling, not while rising.
        if (!isGrounded && rb.linearVelocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f),
                ForceMode.Acceleration);
    }

    private void PlayLandingSquash()
    {
        if (body == null) return;
        if (squashRoutine != null)
            StopCoroutine(squashRoutine);
        squashRoutine = StartCoroutine(Squash());
    }

    private IEnumerator Squash()
    {
        // Preserve volume approximately: if Y is 0.7, X and Z are 1/sqrt(0.7).
        float horizontal = 1f / Mathf.Sqrt(squashY);
        Vector3 squashed = Vector3.Scale(bodyRestScale,
            new Vector3(horizontal, squashY, horizontal));

        for (float elapsed = 0f; elapsed < squashDuration; elapsed += Time.deltaTime)
        {
            float p = Ease.OutBack(elapsed / squashDuration);
            body.localScale = Vector3.LerpUnclamped(squashed, bodyRestScale, p);
            yield return null;
        }

        body.localScale = bodyRestScale; // Exact resting pose, even with overshoot.
        squashRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Application.isPlaying && isGrounded ? Color.green : Color.yellow;
        Vector3 center = feet != null
            ? feet.position
            : transform.position + Vector3.down * 0.9f;
        Gizmos.DrawWireSphere(center, checkRadius);
    }
}
