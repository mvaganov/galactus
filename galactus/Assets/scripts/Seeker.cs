using UnityEngine;
using System.Collections;

public class Seeker : MonoBehaviour {

    public Transform target;
    public float maxSpeed = 10;
    public float maxAcceleration = 5;
    [Tooltip("from 0 (no wobble) to 1 (no coherence)")]
    public float randomWobble = 0.025f;
    Rigidbody rb;

    public void Setup(Transform t, float speed, float accel)
    {
        target = t;
        maxSpeed = speed;
        maxAcceleration = accel;
        rb = GetComponent<Rigidbody>();
    }

	void Update () {
        if (target)
        {
            Vector3 accelForce = target.position - transform.position;
            accelForce.Normalize();
            accelForce *= maxAcceleration;
            rb.velocity += accelForce * Time.deltaTime;
            float speed = rb.velocity.magnitude;
            if (speed > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }
    }
}
