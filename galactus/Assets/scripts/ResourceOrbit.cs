using UnityEngine;

public class ResourceOrbit : MonoBehaviour {

    private ResourceEater target;
    float maxSpeed = 10;
    float maxAcceleration = 5;
    Rigidbody rb;

    public void SetTerminalVelocity(float speed) { maxSpeed = speed; }
    public float GetTerminalVelocity() { return maxSpeed; }

    public void SetForce(float acceleration) { maxAcceleration = acceleration; }
    public float GetForce() { return maxAcceleration; }

    public void Setup(ResourceEater t, float speed, float accel)
    {
        target = t;
        maxSpeed = speed;
        maxAcceleration = accel;
        rb = GetComponent<Rigidbody>();
    }

	void FixedUpdate () {
        float speed = rb.velocity.magnitude;
        if (target) {
            Vector3 accelForce = target.transform.position - transform.position;
            accelForce.Normalize();
            accelForce *= maxAcceleration;
            rb.velocity += accelForce * Time.deltaTime;
            if (speed > maxSpeed) {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
            if (!target.IsAlive()) {
                target = null;
                ResourceNode rn = GetComponent<ResourceNode>();
                if (rn) rn.RefreshSize();
            }
        } else if(speed > 0) {
            float accelThisTime = maxAcceleration * Time.deltaTime;
            if (speed > accelThisTime) {
                rb.velocity = rb.velocity - rb.velocity.normalized * accelThisTime;
            } else {
                rb.velocity = Vector3.zero;
                ResourceNode rn = GetComponent<ResourceNode>();
                if (rn) {
                    rn.RefreshSize();
                    if (!target) rn.creator = null;
                }
            }
        }
    }
}
