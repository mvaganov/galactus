using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerForce : MonoBehaviour {
	// TODO eaters attack for less damage from eaters with similar colors. the same color means essentially alliance.
	// TODO some kind of ray attack that forces hit target to release energy

	public float maxAcceleration = 10;
	public float maxSpeed = 20;
	public bool showDebugLines = false, flee = false;
    public Vector3 accelDirection;
	/// <summary>movement decision making (user input)</summary>
	public float fore = 1, side;
    private UserSoul soul = null;
    public GameObject target;
    float distanceFromTarget;
    float timer = 0;
    private Vector3 flyErrorVector;

    private Rigidbody rb;
    private ResourceEater res;
    private MeshRenderer meshRend;


    [System.Serializable]
    public class AISettings
    {
        [Tooltip("what percent error this agent should experience when just flying around")]
        public float minFlyError = 0, maxFlyError = 1, currentFlyError = 1;
        public void Reset() {
            if(minFlyError < maxFlyError) { currentFlyError = Random.Range(minFlyError, maxFlyError); }
        }
    }
    public AISettings aISettings = new AISettings();
	public ResourceEater GetResourceEater() { if (!res) FindComponents (); return res; }
	public MeshRenderer GetMeshRenderer() { if (!meshRend) FindComponents (); return meshRend; }
	public Rigidbody GetRigidBody() { if (!rb) FindComponents (); return rb; }
	public SphereCollider GetCollisionSphere() { return GetComponent<SphereCollider> (); }
    public void SetUserSoul(UserSoul soul) { this.soul = soul; }
    public UserSoul GetUserSoul() { return soul; }

    private void FindComponents() {
		for (int i = 0; (!meshRend || !res) && i < transform.childCount; ++i) {
			if(!meshRend) meshRend = transform.GetChild (i).GetComponent<MeshRenderer> ();
			if(!res) res = transform.GetChild(i).GetComponent<ResourceEater>();
		}
		if (!meshRend) meshRend = GetComponent<MeshRenderer> ();
		if(!rb) rb = GetComponent<Rigidbody>();
	}

	public bool IsDead() { return res.IsDead (); }
	public bool IsAlive() { return res.IsAlive (); }
	public void Rebirth() {
        aISettings.Reset();
        if (!res) FindComponents ();
		res.resetValues ();
		GetCollisionSphere ().isTrigger = false;
	}
	public void Die(){ res.Die (); }

	void Start () {
		FindComponents ();
		rb.freezeRotation = true;
    }

	void TurnModelTowardCorrectDirection(float t){
		Vector3 dir = accelDirection;
		if (dir == Vector3.zero) {
			dir = GetComponent<Rigidbody> ().velocity.normalized;
		}
		if (dir != Vector3.zero) {
			Vector3 up = transform.up;
			if (soul)
				up = soul.cameraTransform.up;
			Quaternion lookDir = Quaternion.LookRotation (dir, up);
			transform.rotation = Quaternion.Lerp (transform.rotation, lookDir, t);
		}
	}

	void FixedUpdate() {
		TurnModelTowardCorrectDirection (Time.deltaTime);
		if (IsAlive ()) {
			DoSteering (this);
		}
		DoPhysics ();
	}


	public void ClearTarget() { target = null; distanceFromTarget = -1; }

	public bool FollowThisTargetIfItsCloser(GameObject t) {
        float d = Vector3.Distance(transform.position, t.transform.position) - (transform.lossyScale.x + t.transform.lossyScale.x) / 2;
        if (d <= 0) return false;
        // if there is no target, or this target is closer
        if (distanceFromTarget < 0 || d < distanceFromTarget){
            distanceFromTarget = d;
            target = t;
            return true;
        }
		return false;
	}

	void DoSteering(PlayerForce ml) {
		Rigidbody rb = GetComponent<Rigidbody>();
		// player control
		if (soul) {
			Vector3 dir = Vector3.zero;
			//controllingTransform.forward
			fore = Input.GetAxis ("Vertical");
			side = Input.GetAxis ("Horizontal");
			if (fore != 0 || side != 0) { 
				if (fore != 0) { 
					dir = Steering.SeekDirectionNormal (soul.cameraTransform.forward * ml.maxSpeed, rb.velocity, ml.maxAcceleration, Time.deltaTime);
					dir *= fore; 
				}
				if (side != 0) {
					Vector3 right = Vector3.Cross (soul.cameraTransform.up, dir);
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
                flyErrorVector = Random.insideUnitSphere * aISettings.currentFlyError;
                if (target == World.GetInstance()) target = null;
				Ray r = new Ray(transform.position, Random.onUnitSphere);
				RaycastHit[] hits = Physics.SphereCastAll(r, thisRe.GetRadius()+20, 100f);
				if (hits != null && hits.Length > 0) {
                    if (target)
                    {
                        GameObject lastTarget = target;
                        ClearTarget();
                        FollowThisTargetIfItsCloser(lastTarget);
                    }
                    foreach (RaycastHit hit in hits) {
						ResourceNode n = hit.collider.GetComponent<ResourceNode>();
						if (n) {
							if (FollowThisTargetIfItsCloser(hit.collider.gameObject)) { flee = false; }
							timer = Random.Range(2.0f, 3.0f);
						} else {
							ResourceEater re = hit.collider.GetComponent<ResourceEater>();
							if (re && re != thisRe) {
								if (re.GetMass() > thisRe.GetMass() * ResourceEater.minimumPreySize) {
									if (FollowThisTargetIfItsCloser(re.gameObject)) {
										flee = true;
										timer = Random.Range(0.25f, 3.0f);
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
                Vector3 targetPosition = target.transform.position;
                if (showDebugLines) {
                    LineRenderer lr = Lines.Make(ref line_target, Color.magenta, transform.position, targetPosition, .2f, 0);
                    lr.transform.SetParent(transform);
                }
                targetPosition += flyErrorVector * distanceFromTarget;
                if (showDebugLines) {
                    LineRenderer lr = Lines.Make(ref line_error, Color.green, transform.position, targetPosition, .5f, .1f);
                    lr.transform.SetParent(transform);
                }
                if (!flee) {
					steerForce = Steering.Arrive(transform.position, targetPosition, rb.velocity, ml.maxSpeed, ml.maxAcceleration, Time.deltaTime);
				} else {
					steerForce = Steering.Flee(transform.position, targetPosition, rb.velocity, ml.maxSpeed, ml.maxAcceleration, Time.deltaTime);
				}
				transform.LookAt(transform.position + steerForce);
				ml.accelDirection = steerForce;
			}
		}
	}

	void DoPhysics() {
		bool noAccel = accelDirection == Vector3.zero;
		float currentSpeed = rb.velocity.magnitude;
		if (noAccel && currentSpeed == 0) return;
		Vector3 accelForce = accelDirection * maxAcceleration;
		Vector3 direction = rb.velocity / currentSpeed;
		// if we aren't moving at all, reverse!
		if(noAccel && currentSpeed != 0) {
			// this will prevent spastic acceleration force
			float forceRequiredToStop = currentSpeed / Time.deltaTime;
			if(forceRequiredToStop > maxAcceleration) {
				accelForce = -direction * maxAcceleration;
			} else {
				accelForce = -direction * forceRequiredToStop;
			}
		}
		// how aligned our acceleration is with our velocity: are we trying to go faster? negative = no.
		float speedChange = Vector3.Dot(accelForce, rb.velocity);
		if(accelForce != Vector3.zero) {
			// reduce the acceleration force based on mass
			accelForce /= rb.mass;
			float currentMaxSpeed = maxSpeed / rb.mass;
			if (currentMaxSpeed > World.SPEED_LIMIT) currentMaxSpeed = World.SPEED_LIMIT;
			// if we're going too fast, and trying to go faster
			if (currentSpeed >= currentMaxSpeed && speedChange > 0) {
				// reduce our acceleration force in our current speed direction
				float overSpeed = Vector3.Dot(direction, accelForce);
				accelForce -= direction * overSpeed;
			}
			rb.velocity += accelForce * Time.deltaTime;
		}
		if(showDebugLines) {
			Vector3 p = transform.position;
			Vector3 v = p + rb.velocity;
			LineRenderer lr = Lines.Make(ref line_velocity, Color.cyan, p, v, .1f, .1f);
			lr.transform.SetParent (transform);
			lr = Lines.Make(ref line_acceleration, Color.magenta, v, v + accelForce, .2f, 0);
			lr.transform.SetParent (transform);
		}
	}
	private GameObject line_velocity, line_acceleration, line_target, line_error;
}
