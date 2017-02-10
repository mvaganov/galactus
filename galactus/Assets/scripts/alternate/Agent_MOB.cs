//#define SHOWVECTORS
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Agent_MOB : MonoBehaviour {

	// TODO add obstacle avoidance, flocking, and path-finding code

	public float accelerationForce = 10, maxSpeed = 20, turnSpeed = 180;
	private Vector3 directionToMoveToward, directionTowardTarget;

	public enum TargetBehavior {none, seek, arrive, flee, stop};
	public enum AccelerationApplies {inAnyDirection, onlyForwardAndStop};

	public TargetBehavior targetBehavior = TargetBehavior.arrive;
	public AccelerationApplies accelerationApplies = AccelerationApplies.inAnyDirection;
	[SerializeField]
	private EatSphere eatSphere;
	public EatSphere GetEatSphere() { return eatSphere; }

#if SHOWVECTORS
	[HideInInspector]
	GameObject directionLine, desiredChangeLine, targetLine;
#endif
	public Vector3 target;
	[HideInInspector]
	public Rigidbody rb;

	public void EnsureRigidBody() {
		if(rb == null) rb = GetComponent<Rigidbody> ();
	}

	public Vector3 GetVelocity() {
		return rb.velocity;
	}

	void Start () {
		EnsureRigidBody ();
	}

	private Vector3 Seek() {
		Vector3 desiredVelocity = directionTowardTarget * maxSpeed;
		Vector3 desiredChangeToVelocity = desiredVelocity - rb.velocity;
		Vector3 steerDirection = desiredChangeToVelocity.normalized;
		return steerDirection;
	}

	Vector3 CalculateSeekAcceleration() {
		Vector3 delta = target - transform.position;
		if (delta == Vector3.zero) return Vector3.zero;
		float desiredDistance = delta.magnitude;
		directionTowardTarget = delta / desiredDistance;
		return Seek ();
	}

	Vector3 CalculateArriveAcceleration () {
		Vector3 delta = target - transform.position;
		if (delta == Vector3.zero) return Vector3.zero;
		float desiredDistance = delta.magnitude;
		if (desiredDistance > 1.0 / 1024) {
			float currentSpeed = rb.velocity.magnitude;
			float brakeDistance = BrakeDistance (currentSpeed, accelerationForce);
			if (brakeDistance < (desiredDistance - currentSpeed*Time.deltaTime)) {
				directionTowardTarget = delta / desiredDistance;
				return Seek ();
			}
		}
		return ApplyBrakes ();
	}

	public static float BrakeDistance(float speed, float acceleration) {
		return (speed * speed) / (2 * acceleration);
	}

	public Vector3 ApplyBrakes() {
		if (rb.velocity != Vector3.zero) {
			float speed = rb.velocity.magnitude;
			// just stop if the acceleration force required is small enough.
			if (speed < accelerationForce * Time.deltaTime) {
				directionToMoveToward = Vector3.zero;
				rb.velocity = Vector3.zero;
				#if SHOWVECTORS
				Lines.Make (ref directionLine, transform.position, transform.position, Color.cyan);
				Lines.Make (ref desiredChangeLine, transform.position, transform.position, Color.red, .1f, 0);
				directionLine.transform.parent = transform;
				desiredChangeLine.transform.parent = transform;
				#endif
			} else {
				directionToMoveToward = -rb.velocity / speed;
			}
		} else {
			directionToMoveToward = Vector3.zero;
		}
		return directionToMoveToward;
	}

	public void ApplyForceToward(Vector3 direction) {
		//rb.velocity += direction * accelerationForce * Time.deltaTime;
		rb.AddForce (direction * accelerationForce);
		// enforce speed limit
		float currentSpeed = rb.velocity.magnitude;
		if (currentSpeed > maxSpeed) {
			rb.velocity = rb.velocity.normalized * maxSpeed;
		}
		#if SHOWVECTORS
		Vector3 forwardPoint = transform.position + rb.velocity;
		Lines.Make (ref directionLine, transform.position, forwardPoint, Color.cyan);
		Lines.Make (ref desiredChangeLine, forwardPoint, forwardPoint+direction*accelerationForce, Color.red, .1f, 0);
		directionLine.transform.parent = transform;
		desiredChangeLine.transform.parent = transform;
		#endif
	}

	// Update is called once per frame
	void FixedUpdate () {
		// calculate how to change velocity
		switch(targetBehavior) {
		case TargetBehavior.seek:   directionToMoveToward = CalculateSeekAcceleration (); break;
		case TargetBehavior.arrive: directionToMoveToward = CalculateArriveAcceleration (); break;
		case TargetBehavior.flee:   directionToMoveToward =-CalculateSeekAcceleration (); break;
		case TargetBehavior.stop:   directionToMoveToward = ApplyBrakes(); break;
		}
		UpdateLookDirection ();
		#if SHOWVECTORS
		Lines.Make (ref targetLine, transform.position, target, Color.red);
		targetLine.transform.parent = transform;
		#endif
		DoPhysics ();
	}

	void DoPhysics() {
		// apply change to velocity
		if(directionToMoveToward == Vector3.zero) { return; };
		ApplyForceToward (directionToMoveToward);
	}

	/// <summary>turn toward where this agent wants to look.</summary>
	public void UpdateLookDirection() {
		if (directionTowardTarget == Vector3.zero) { return; }
		Vector3 lookUp;
		if (transform.forward != directionTowardTarget) {
			lookUp = Vector3.Cross (transform.forward, directionTowardTarget);
			if (lookUp == Vector3.zero) {
				lookUp = transform.up;
			} else {
				lookUp.Normalize ();
			}
			UpdateLookDirection (directionTowardTarget, lookUp);
		}
	}
	public void UpdateLookDirection(Vector3 lookForward, Vector3 lookUp) {
		if(lookForward != transform.forward && lookForward != Vector3.zero) {
			transform.rotation = Quaternion.RotateTowards (transform.rotation, 
				Quaternion.LookRotation (lookForward, lookUp), turnSpeed * Time.deltaTime);
		}
	}
}
