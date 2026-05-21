using UnityEngine;

public class BowlingPinControllerVERIFIER : MonoBehaviour
{
    [Header("Pin Physical Variables")]
    [Range(0.25f, 1f)] [SerializeField] private float pinRadius = 0.5f;
    [Range(0.5f, 2f)]  [SerializeField] private float pinHeight = 2f;
    [Range(1f, 5f)]    [SerializeField] private float pinMass = 2f;

    private Vector3 pinVelocity = Vector3.zero;
    private Vector3 angularVelocity = Vector3.zero;

    private Vector3 pos;

    private Vector3 up = Vector3.up;

    private Quaternion currentRotation = Quaternion.identity;

    private float inertia;

    private Quaternion finalRotation;
    private Vector3 startPosition;
    private Vector3 storedCorrectionDelta;

    public float GetPinRadius() { return pinRadius; }
    public float GetPinMass() { return pinMass; }
    public Vector3 GetPinVelocity() { return pinVelocity; }

    public Vector3 GetTopPoint() { return pos + (GetUpDir() * ((pinHeight * 0.5f) - pinRadius)); }
    public Vector3 GetBottomPoint() { return pos - (GetUpDir() * ((pinHeight * 0.5f) - pinRadius)); }

    private Vector3 GetUpDir() { return currentRotation * up; }

    // Reset ball and pos at the start for safety
    private void Start()
    {
        startPosition = transform.position;

        ResetPin();
    }

    // Resets the pins's variables to being held so it can be hit again
    public void ResetPin()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.Sleep();

        transform.position = startPosition;
        pos = transform.position;

        currentRotation = Quaternion.identity;

        transform.rotation = currentRotation;

        finalRotation = Quaternion.identity;
        storedCorrectionDelta = Vector3.zero;

        pinVelocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        inertia = (1f / 12f) * pinMass * (3 * pinRadius * pinRadius + pinHeight * pinHeight); // Computes moment of intertia for the pin (which is a cylinder-like object)
    }

    private void FixedUpdate()
    {
        pos = transform.position;
        pos = pos + (pinVelocity * Time.deltaTime);

        ApplyConstraints(ref pos);

        ApplyTransform(pos);
    }

    private void ApplyConstraints(ref Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, -WorldData.laneWidth + pinRadius, WorldData.laneWidth - pinRadius);

        pos.z = Mathf.Clamp(pos.z, 0 + pinRadius, WorldData.laneDepth - pinRadius);
    }

    private void ApplyTransform(Vector3 pos)
    {
        transform.position = pos;
    }
}