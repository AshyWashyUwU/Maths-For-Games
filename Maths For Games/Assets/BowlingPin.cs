using UnityEngine;

public class BowlingPin : MonoBehaviour
{
    public bool isHit;

    private CustomMathsLibrary.Vector3 velocity = CustomMathsLibrary.Vector3.zero;
    private float fallSpeed;

    public void ApplyHitForce(CustomMathsLibrary.Vector3 force)
    {
        isHit = true;
        velocity = force;
        fallSpeed = CustomMathsLibrary.Magnitude(force);
    }

    private void Update()
    {
        if (!isHit) return;

        float verticalVelocity = 0;

        verticalVelocity += CustomPhysicsLibrary.GRAVITY_FORCE * Time.deltaTime;

        velocity.y = verticalVelocity;

        if (transform.position.y <= -2f)
        {
            transform.position = new CustomMathsLibrary.Vector3(transform.position.x, -2f, transform.position.z);
            verticalVelocity = 0f;
        }

        transform.Rotate(Vector3.right * fallSpeed * Time.deltaTime * 50f);
    }
}
