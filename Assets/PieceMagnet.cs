using UnityEngine;

public class PieceMagnet : MonoBehaviour
{
    public float startMagnetHeight = 0.6f;
    public float magnetForce = 40f;
    public float damping = 8f;
    public float snapDistance = 0.03f;
    public float snapSpeed = 0.2f;

    Rigidbody rb;
    Vector3 targetPos;
    bool hasTarget, magnetOn;

    void Awake() => rb = GetComponent<Rigidbody>();

    public void SetTarget(Vector3 center)
    {
        targetPos = center;
        hasTarget = true;
    }

    void FixedUpdate()
    {
        if (!hasTarget || rb.isKinematic) return;

        float height = transform.position.y - targetPos.y;
        if (!magnetOn && height <= startMagnetHeight) magnetOn = true;
        if (!magnetOn) return;

        Vector3 toTarget = targetPos - rb.position;
        Vector3 force = toTarget * magnetForce - rb.linearVelocity * damping;
        rb.AddForce(force, ForceMode.Acceleration);

        if (toTarget.magnitude <= snapDistance && rb.linearVelocity.magnitude <= snapSpeed)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = targetPos;
            rb.rotation = Quaternion.Euler(0f, rb.rotation.eulerAngles.y, 0f);
            rb.isKinematic = true;
        }
    }
}
