using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerForce : MonoBehaviour {
	Rigidbody rb;
	public float accelerationForce = 10;
	public float maxSpeed = 20;
	public bool playerControlled = true;
	public bool showDebugLines = false;
	GameObject line_velocity, line_acceleration;
    [Tooltip("What transform to use with WASD controls (default to self if null)")]
    public Transform orientation;
	void Start () {
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;
        if (!orientation) orientation = transform;

    }
	float stuckTimer = -1;
	Vector3 stuckPosition;

    public float fore = 1, side;

	void Update () {
		if(playerControlled) {
			fore = Input.GetAxis("Vertical");
			side = Input.GetAxis("Horizontal");
		} else {
			// if the AI controlled player seems stuck, turn them around
			if(rb.velocity.magnitude < 0.001f) {
				stuckPosition = transform.position;
				stuckTimer = 0;
			}
			if(stuckTimer >= 0) {
				stuckTimer += Time.deltaTime;
				transform.LookAt(stuckPosition);
				transform.Rotate(0, 180, 0);
				if(stuckTimer > 1) {
					stuckTimer = -1;
				}
			}
		}
		Vector3 accel = Vector3.zero;
		if(fore != 0) {
			accel += orientation.forward * fore * accelerationForce;
		}
		if(side != 0) {
			accel += orientation.right * side * accelerationForce;
		}
		float speed = rb.velocity.magnitude;
		Vector3 direction = rb.velocity / speed;
		// if we aren't moving at all, reverse!
		if(side == 0 && fore == 0 && speed != 0) {
			// this will prevent spastic acceleration force
			float forceRequiredToStop = speed / Time.deltaTime;
			if(forceRequiredToStop > accelerationForce) {
				accel = -direction * accelerationForce;
			} else {
				accel = -direction * forceRequiredToStop;
			}
		}
		// how aligned our acceleration is with our velocity: are we trying to go faster? negative = no.
		float speedChange = Vector3.Dot(accel, rb.velocity);
		// if we are slowing down, OR we aren't going our max speed
		if(accel != Vector3.zero) {
            float currentMaxSpeed = maxSpeed / rb.mass;
            if (currentMaxSpeed > World.SPEED_LIMIT) currentMaxSpeed = World.SPEED_LIMIT;
            // if we're going too fast, and trying to go faster
            if (speed >= currentMaxSpeed && speedChange > 0) {
				// reduce our acceleration force in our current speed direction
				float overSpeed = Vector3.Dot(direction, accel);
				accel -= direction * overSpeed;
			} else
            {
                accel /= rb.mass;
            }
			rb.velocity += accel * Time.deltaTime;
		}
		if(showDebugLines) {
			Vector3 p = transform.position;
			Vector3 v = p + rb.velocity;
			Lines.Make(ref line_velocity, Color.cyan, p, v, .1f, .1f);
			Lines.Make(ref line_acceleration, Color.magenta, v, v + accel, .2f, 0);
		}
	}
}
