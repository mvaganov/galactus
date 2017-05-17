//#define SHOWVECTORS
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Agent_MOB : MonoBehaviour {

	// TODO add obstacle avoidance, flocking, and path-finding code
	public float acceleration = 5, maxSpeed = 20, turnSpeed = 180;
	private Vector3 accelerationDirection;
	/// <summary>cached variables required for a host of calculations</summary>
	private float currentSpeed, brakeDistance;
	private bool brakesOn = false, brakesOnLastFrame = false;
#if SHOWVECTORS
	[HideInInspector]
	GameObject directionLine, desiredChangeLine, targetLine;
#endif
	[HideInInspector]
	public Rigidbody rb;

	public Vector3 GetAccelerationDirection() {
		return accelerationDirection;
	}

	public bool IsBraking() {
		return brakesOn || brakesOnLastFrame;
	}

	public void EnsureRigidBody() {
		if(rb == null) rb = GetComponent<Rigidbody> ();
	}

	public Vector3 GetVelocity() {
		return rb.velocity;
	}

	void Start () {
		EnsureRigidBody ();
	}

	void FixedUpdate () {
		currentSpeed = rb.velocity.magnitude;
		brakeDistance = BrakeDistance(currentSpeed, acceleration/rb.mass)-(currentSpeed*Time.deltaTime);
		if (brakesOn) {
			brakesOnLastFrame = true;
			brakesOn = false;
		} else {
			brakesOnLastFrame = false;
		}
	}

	public float GetSpeed() {
		return currentSpeed;
	}

	public float GetBrakeDistance() {
		return brakeDistance;
	}

	private Vector3 SeekMath(Vector3 directionToLookToward) {
		Vector3 desiredVelocity = directionToLookToward * maxSpeed;
		Vector3 desiredChangeToVelocity = desiredVelocity - rb.velocity;
		Vector3 steerDirection = desiredChangeToVelocity.normalized;
		return steerDirection;
	}

	public void CalculateSeek(Vector3 target, ref Vector3 directionToMoveToward, ref Vector3 directionToLookToward) {
		Vector3 delta = target - transform.position;
		if (delta == Vector3.zero) { return; }
		float desiredDistance = delta.magnitude;
		directionToLookToward = delta / desiredDistance;
		directionToMoveToward = SeekMath (directionToLookToward);
	}

	public void CalculateArrive (Vector3 target, ref Vector3 directionToMoveToward, ref Vector3 directionToLookToward) {
		Vector3 delta = target - transform.position;
		if (delta == Vector3.zero) { return; }
		float desiredDistance = delta.magnitude;
		if (desiredDistance > 1.0 / 1024) {
//			float currentSpeed = rb.velocity.magnitude;
//			float brakeDistance = BrakeDistance (currentSpeed, accelerationForce);
//			float spaceToBrakeIn = desiredDistance-(currentSpeed*Time.deltaTime);
			if (brakeDistance < desiredDistance) {
				directionToLookToward = delta / desiredDistance;
				directionToMoveToward = SeekMath (directionToLookToward);
				return;
			}
		}
		directionToMoveToward = ApplyBrakes ();
	}

	public static float BrakeDistance(float speed, float acceleration) {
		return (speed * speed) / (2 * acceleration);
	}

	public Vector3 ApplyBrakes() {
		brakesOn = true;
		Vector3 directionToMoveToward = Vector3.zero;
		if (rb.velocity != Vector3.zero) {
			float speed = rb.velocity.magnitude;
			// just stop if the acceleration force required is small enough.
			if (speed < acceleration * Time.deltaTime) {
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
		}
		return directionToMoveToward;
	}

	public void Seek(Vector3 target) {
		Vector3 directionToMoveToward = Vector3.zero, directionToLookToward = Vector3.zero;
		CalculateSeek (target, ref directionToMoveToward, ref directionToLookToward);
		UpdateLookDirection (directionToLookToward);
		ApplyForceToward (directionToMoveToward);
	}

	public void Flee(Vector3 target) {
		Vector3 directionToMoveToward = Vector3.zero, directionToLookToward = Vector3.zero;
		CalculateSeek (target, ref directionToMoveToward, ref directionToLookToward);
		UpdateLookDirection (directionToLookToward);
		ApplyForceToward (-directionToMoveToward);
	}

	public void Arrive(Vector3 target) {
		Vector3 move = Vector3.zero, look = Vector3.zero;
		CalculateArrive (target, ref move, ref look);
		UpdateLookDirection (look);
		ApplyForceToward (move);
	}

	public void Brake() {
		Vector3 directionToMoveToward = ApplyBrakes(); 
		ApplyForceToward (directionToMoveToward);
	}

	public void RandomLook() {
		Vector3 direction = Random.onUnitSphere;
		UpdateLookDirection (direction);
	}

	public void RandomWalk() {
		Vector3 direction = Random.onUnitSphere;
		UpdateLookDirection (direction);
		ApplyForceToward (transform.forward+direction);
		#if SHOWVECTORS
		Lines.Make (ref targetLine, transform.position, transform.position+direction*maxSpeed, Color.red);
		targetLine.transform.parent = transform;
		#endif
	}

	public void RandomWalk(Vector3 weightedToward, float weight) {
		Vector3 dir = weightedToward - transform.position;
		dir.Normalize ();
		dir = (dir * weight) + (Random.onUnitSphere * (1 - weight));
		dir.Normalize ();
		UpdateLookDirection (dir);
		ApplyForceToward (transform.forward+dir);
		#if SHOWVECTORS
		Lines.Make (ref targetLine, transform.position, transform.position+direction*maxSpeed, Color.red);
		targetLine.transform.parent = transform;
		#endif
	}

	public void ApplyForceToward(Vector3 directionToMoveToward) {
		accelerationDirection = directionToMoveToward;
		// apply change to velocity
		if(directionToMoveToward == Vector3.zero) { return; };
		//rb.velocity += direction * accelerationForce * Time.deltaTime;
		rb.AddForce (directionToMoveToward * acceleration);
		// enforce speed limit
		float currentSpeed = rb.velocity.magnitude;
		if (currentSpeed > maxSpeed) {
			rb.velocity = rb.velocity.normalized * maxSpeed;
		}
		#if SHOWVECTORS
		Vector3 forwardPoint = transform.position + rb.velocity;
		Lines.Make (ref directionLine, transform.position, forwardPoint, Color.cyan);
		Lines.Make (ref desiredChangeLine, forwardPoint, forwardPoint+directionToMoveToward*accelerationForce, Color.red, .1f, 0);
		directionLine.transform.parent = transform;
		desiredChangeLine.transform.parent = transform;
		#endif
	}

	/// <summary>turn toward where this agent wants to look.</summary>
	public void UpdateLookDirection(Vector3 directionToLookToward) {
		if (directionToLookToward == Vector3.zero) { return; }
		Vector3 lookUp;
		if (transform.forward != directionToLookToward) {
			lookUp = Vector3.Cross (transform.forward, directionToLookToward);
			if (lookUp == Vector3.zero) {
				lookUp = transform.up;
			} else {
				lookUp.Normalize ();
			}
			UpdateLookDirection (directionToLookToward, lookUp);
		}
	}
	public void UpdateLookDirection(Vector3 lookForward, Vector3 lookUp) {
		if(lookForward != transform.forward && lookForward != Vector3.zero) {
			transform.rotation = Quaternion.RotateTowards (transform.rotation, 
				Quaternion.LookRotation (lookForward, lookUp), turnSpeed * Time.deltaTime);
		}
	}

	public float DistanceTo(Vector3 loc) {
		return Vector3.Distance (transform.position, loc) - transform.localScale.z;
	}

	public float DistanceTo(Transform mob) {
		return Vector3.Distance (transform.position, mob.transform.position) - (transform.localScale.z+mob.transform.localScale.z);
	}
}
