using UnityEngine;

// Lecture 04 CameraFollow upgraded using Lecture 05's exact damping and look-ahead.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerController target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -7f);
    [SerializeField, Min(0.01f)] private float sharpness = 5f;
    [SerializeField, Min(0f)] private float lookAhead = 0.35f; // seconds
    [SerializeField] private float lookHeight = 1f;

    private void Awake()
    {
        if (target == null)
            target = FindAnyObjectByType<PlayerController>();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            target = FindAnyObjectByType<PlayerController>();
            if (target == null) return;
        }

        Vector3 flatVelocity = target.PlayerRigidbody.linearVelocity;
        flatVelocity.y = 0f;

        // Predict where the player is heading, not just where they are.
        Vector3 aim = target.transform.position
                      + Vector3.up * lookHeight
                      + flatVelocity * lookAhead;
        Vector3 wanted = aim + offset;

        // Frame-rate-independent smoothing from Lecture 05, slide 42.
        float blend = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, wanted, blend);
        transform.LookAt(aim);
    }
}
