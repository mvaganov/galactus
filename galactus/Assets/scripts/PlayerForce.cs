using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerForce : MonoBehaviour {
	Rigidbody rb;
	public float maxAcceleration = 10;
	public float maxSpeed = 20;
	public bool playerControlled = true;
	public bool showDebugLines = false;
	public Vector3 accelDirection;

    public ResourceEater GetResourceEater() {
        ResourceEater re = null;
        for (int i = 0; re == null && i < transform.childCount; ++i)
            re = transform.GetChild(i).GetComponent<ResourceEater>();
        return re;
    }

	void Start () {
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;
    }

	public float fore = 1, side;

	void TurnModelTowardCorrectDirection(float t){
		Vector3 dir = accelDirection;
		if (dir == Vector3.zero) {
			dir = GetComponent<Rigidbody> ().velocity.normalized;
		}
		if (dir != Vector3.zero) {
			Vector3 up = transform.up;
			if (controllingTransform)
				up = controllingTransform.up;
			Quaternion lookDir = Quaternion.LookRotation (dir, up);
			//transform.LookAt (transform.position + transform.forward + accelDirection, up);
			transform.rotation = Quaternion.Lerp (transform.rotation, lookDir, t);
		}
	}

	void FixedUpdate() {
		TurnModelTowardCorrectDirection (Time.deltaTime);
	}

	public Transform controllingTransform = null;
	public GameObject target;
	float timer = 0;
	public bool flee = false;

	public void ClearTarget() { target = null; }

	public bool AisCloserThanB(GameObject A, GameObject B) {
		return Vector3.Distance(transform.position, A.transform.position) - (transform.lossyScale.x + A.transform.lossyScale.x) / 2
			< Vector3.Distance(transform.position, B.transform.position) - (transform.lossyScale.x + B.transform.lossyScale.x) / 2;
	}

	public bool FollowThisTargetIfItsCloser(GameObject t) {
		// if there is no target, this one is it. de-facto.
		if (!target) { target = t; return true; }
		// if this newly found target is closer than the current target
		else if (AisCloserThanB(t, target)) { target = t; return true; }
		return false;
	}

	void DoSteering(PlayerForce ml) {
		Rigidbody rb = GetComponent<Rigidbody>();
		// player control
		if (controllingTransform) {
			Vector3 dir = Vector3.zero;
			//controllingTransform.forward
			fore = Input.GetAxis ("Vertical");
			side = Input.GetAxis ("Horizontal");
			if (fore != 0 || side != 0) { 
				if (fore != 0) { 
					dir = Steering.SeekDirectionNormal (controllingTransform.forward * ml.maxSpeed, rb.velocity, ml.maxAcceleration, Time.deltaTime);
					dir *= fore; 
				}
				if (side != 0) {
					Vector3 right = Vector3.Cross (controllingTransform.up, dir);
					dir += right * side * (fore < 0 ? -1 : 1);
				}
				if (fore != 0 && side != 0) {
					dir.Normalize ();
				}
			}
			ml.accelDirection = dir;
		} else { // AI control
			ResourceEater thisRe = ml.GetResourceEater();
			timer -= Time.deltaTime;
			if (!target || timer <= 0) {
				if (target == World.GetInstance()) target = null;
				Ray r = new Ray(transform.position, Random.onUnitSphere);
				RaycastHit[] hits = Physics.SphereCastAll(r, thisRe.GetRadius()+20, 100f);
				if (hits != null && hits.Length > 0) {
					foreach (RaycastHit hit in hits) {
						ResourceNode n = hit.collider.GetComponent<ResourceNode>();
						if (n) {
							if (FollowThisTargetIfItsCloser(hit.collider.gameObject)) { flee = false; }
							timer = Random.Range(1.0f, 2.0f);
						} else {
							ResourceEater re = hit.collider.GetComponent<ResourceEater>();
							if (re && re != thisRe) {
								if (re.GetMass() > thisRe.GetMass() * ResourceEater.minimumPreySize) {
									if (FollowThisTargetIfItsCloser(re.gameObject)) {
										flee = true;
										timer = Random.Range(0.25f, 2.0f);
									}
								} else if(re.GetMass() * ResourceEater.minimumPreySize < thisRe.GetMass()) {
									if (FollowThisTargetIfItsCloser(re.gameObject)) { 
										flee = false;
										timer = Random.Range(1.0f, 5.0f);
									}
								}
							}
						}

					}
				}
				if (target == null) {
					timer = Random.Range(1.0f, 2.0f);
					target = World.GetInstance().gameObject;
				}
			}
			if (target) {
				Vector3 steerForce = Vector3.zero;
				if (!flee) {
					steerForce = Steering.Arrive(transform.position, target.transform.position, rb.velocity, ml.maxSpeed, ml.maxAcceleration, Time.deltaTime);
				} else {
					steerForce = Steering.Flee(transform.position, target.transform.position, rb.velocity, ml.maxSpeed, ml.maxAcceleration, Time.deltaTime);
				}
				transform.LookAt(transform.position + steerForce);
				ml.accelDirection = steerForce;
			}
		}
	}

	void Update () {
		DoSteering (this);
		DoPhysics ();
	}

	void DoPhysics() {
		Vector3 accel = accelDirection * maxAcceleration;
		float speed = rb.velocity.magnitude;
		Vector3 direction = rb.velocity / speed;
		// if we aren't moving at all, reverse!
		if(side == 0 && fore == 0 && speed != 0) {
			// this will prevent spastic acceleration force
			float forceRequiredToStop = speed / Time.deltaTime;
			if(forceRequiredToStop > maxAcceleration) {
				accel = -direction * maxAcceleration;
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
			LineRenderer lr = Lines.Make(ref line_velocity, Color.cyan, p, v, .1f, .1f);
			lr.transform.SetParent (transform);
			lr = Lines.Make(ref line_acceleration, Color.magenta, v, v + accel, .2f, 0);
			lr.transform.SetParent (transform);
		}
	}
	GameObject line_velocity, line_acceleration;
}
