using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class AgentForce : MonoBehaviour {
//	/// <summary>what the human player controls</summary>
//	private AgentSoul soul = null;
//
//	private Rigidbody rb;
//	private ResourceHolder res;
//	private MeshRenderer meshRend;
//	float timer = 0;
//
//	[System.Serializable]
//	public class Movement {
//	    // TODO state machine for:
//	    //   standard - gather and grow self
//	    //   worker - gather any energy possible and return to leader, emitting down to 1
//	    //   energy delivery - seek out release all energy into specified team member
//	    //   herbivore - seek out only regular resources (and flee from predators)
//	    //   predator - seek out only other team's agents
//	    //   assassin - seek out and devour specified agent, return all energy after prey devoured, or prey is too big.
//	    //   team predator - surround common prey with other team-predators
//
	    public float maxAcceleration = 10;
		public float maxSpeed = 20;
		public bool showDebugLines = false, flee = false;
	    public Vector3 accelDirection;
	    /// <summary>where this agent wants to be headed when forward is pressed</summary>
	    private Vector3 lookDirection;
	    /// <summary>the direciton that the agent is intentionally headed (could be different than lookDirection because of strafe)</summary>
	    private Vector3 intentedDirection;
	    /// <summary>movement decision making (user input)</summary>
	    public float fore = 1, side;
//
//	    public GameObject target;
//	    float distanceFromTarget;
//	    private Vector3 flyErrorVector;
//
//	    public Vector3 GetLookDirection() { return lookDirection; }
//	    public Vector3 GetMoveDirection() { return intentedDirection; }
//
//	    [System.Serializable]
//	    public class AISettings
//	    {
//	        [Tooltip("what percent error this agent should experience when just flying around")]
//	        public float minFlyError = 0, maxFlyError = 1, currentFlyError = 1;
//	        public void Reset() {
//	            if(minFlyError < maxFlyError) { currentFlyError = Random.Range(minFlyError, maxFlyError); }
//	        }
//	    }
//	    public AISettings aISettings = new AISettings();
//
//		public void ClearTarget() { target = null; distanceFromTarget = -1; }
//
//		public void SetTarget(Transform self, GameObject t) { target = t; distanceFromTarget = Vector3.Distance(self.position, t.transform.position); }
//
//		public void TurnModelTowardCorrectDirection(AgentSoul soul, Transform self, float t){
//			Vector3 dir = accelDirection;
//			if (dir == Vector3.zero) {
//				dir = self.GetComponent<Rigidbody> ().velocity.normalized;
//			}
//			if (dir != Vector3.zero) {
//				Vector3 up = self.up;
//				if (soul)
//					up = soul.cameraTransform.up;
//				Quaternion lookDir = Quaternion.LookRotation (dir, up);
//				self.rotation = Quaternion.Lerp (self.rotation, lookDir, t);
//			}
//		}
//
//		public bool FollowThisTargetIfItsCloser(Transform self, GameObject t) {
//			float d = Vector3.Distance(self.position, t.transform.position) - (self.lossyScale.x + t.transform.lossyScale.x) / 2;
//			if (d <= 0) return false;
//			// if there is no target, or this target is closer
//			if (distanceFromTarget < 0 || d < distanceFromTarget){
//				distanceFromTarget = d;
//				target = t;
//				return true;
//			}
//			return false;
//		}
//
//		public void DoSteering(AgentForce ml) {
//			Rigidbody rb = ml.GetComponent<Rigidbody>();
//			// player control
//			if (ml.soul) {
//				intentedDirection = Vector3.zero;
//				if (!ml.soul.holdVector) {
//					fore = Input.GetAxis ("Vertical");
//					side = Input.GetAxis ("Horizontal");
//					lookDirection = ml.soul.cameraTransform.forward;
//				}
//				if (fore != 0 || side != 0) {
//					intentedDirection = Vector3.zero;
//					if (fore != 0) {
//						intentedDirection = Steering.SeekDirectionNormal (lookDirection * ml.movement.maxSpeed, rb.velocity, ml.movement.maxAcceleration, Time.deltaTime);
//						intentedDirection *= fore; 
//					}
//					if (side != 0) {
//						Vector3 right = ml.soul.cameraTransform.right;
//						intentedDirection += right * side;// * (fore < 0 ? -1 : 1);
//					}
//					if (fore != 0 && side != 0) { intentedDirection.Normalize (); }
//				}
//				ml.movement.accelDirection = intentedDirection;
//			} 
////			else { // AI control
////				AgentForce thisRe = this;
////				timer -= Time.deltaTime;
////				if (!target || timer <= 0) {
////					flyErrorVector = Random.insideUnitSphere * aISettings.currentFlyError;
////					if (target == World.GetInstance()) target = null;
////					Ray r = new Ray(transform.position, Random.onUnitSphere);
////					RaycastHit[] hits = Physics.SphereCastAll(r, thisRe.GetSize()+20, 100f);
////					if (hits != null && hits.Length > 0) {
////						if (target)
////						{
////							GameObject lastTarget = target;
////							ClearTarget();
////							FollowThisTargetIfItsCloser(lastTarget);
////						}
////						foreach (RaycastHit hit in hits) {
////							ResourceNode n = hit.collider.GetComponent<ResourceNode>();
////							if (n && n.GetValue() > 0 && n.creator != res && n.IsEdible()) {
////								if (FollowThisTargetIfItsCloser(hit.collider.gameObject)) { flee = false; }
////								timer = Random.Range(2.0f, 3.0f);
////							} else {
////								ResourceEater re = hit.collider.GetComponent<ResourceEater>();
////								if (re && re != thisRe && !thisRe.IsOnSameTeam(re)) {
////									if (re.GetMass() > thisRe.GetMass() * ResourceEater.MINIMUM_PREY_SIZE) {
////										if (FollowThisTargetIfItsCloser(re.gameObject)) {
////											flee = true;
////											timer = Random.Range(0.25f, 3.0f);
////										}
////									} else if(re.GetMass() * ResourceEater.MINIMUM_PREY_SIZE < thisRe.GetMass()) {
////										if (FollowThisTargetIfItsCloser(re.gameObject)) { 
////											flee = false;
////											timer = Random.Range(1.0f, 5.0f);
////										}
////									}
////								}
////							}
////						}
////					}
////					if (target == null) {
////						timer = Random.Range(1.0f, 2.0f);
////						//target = World.GetInstance().gameObject;
////						target = Singleton.Get<GameRules>().gameObject;
////					}
////				}
////				if (target) {
////					Vector3 steerForce = Vector3.zero;
////					Vector3 targetPosition = target.transform.position;
////					if (showDebugLines) {
////						LineRenderer lr = Lines.Make(ref line_target, Color.magenta, transform.position, targetPosition, .2f, 0);
////						lr.transform.SetParent(transform);
////					}
////					targetPosition += flyErrorVector * distanceFromTarget;
////					if (showDebugLines) {
////						LineRenderer lr = Lines.Make(ref line_error, Color.green, transform.position, targetPosition, .5f, .1f);
////						lr.transform.SetParent(transform);
////					}
////					if (!flee) {
////						steerForce = Steering.Arrive(transform.position, targetPosition, rb.velocity, ml.movement.maxSpeed, ml.movement.maxAcceleration, Time.deltaTime);
////						intentedDirection = (targetPosition - transform.position).normalized;
////					} else {
////						steerForce = Steering.Flee(transform.position, targetPosition, rb.velocity, ml.movement.maxSpeed, ml.movement.maxAcceleration, Time.deltaTime);
////						intentedDirection = (transform.position - targetPosition).normalized;
////					}
////					transform.LookAt(transform.position + steerForce);
////					ml.movement.accelDirection = steerForce;
////				}
////			}
//		}
//
//		public void DoPhysics(AgentForce af) {
//			bool noAccel = accelDirection == Vector3.zero;
//			float currentSpeed = af.rb.velocity.magnitude;
//			if (noAccel && currentSpeed == 0) return;
//			Vector3 accelForce = accelDirection * maxAcceleration;
//			Vector3 direction = af.rb.velocity / currentSpeed;
//			// if we aren't moving at all, reverse!
//			if(noAccel && currentSpeed != 0) {
//				// this will prevent spastic acceleration force
//				float forceRequiredToStop = currentSpeed / Time.deltaTime;
//				if(forceRequiredToStop > maxAcceleration) {
//					accelForce = -direction * maxAcceleration;
//				} else {
//					accelForce = -direction * forceRequiredToStop;
//				}
//			}
//			// how aligned our acceleration is with our velocity: are we trying to go faster? negative = no.
//			float speedChange = Vector3.Dot(accelForce, af.rb.velocity);
//			if(accelForce != Vector3.zero) {
//				// reduce the acceleration force based on mass
//				accelForce /= af.rb.mass;
//				float currentMaxSpeed = maxSpeed / af.rb.mass;
//				if (currentMaxSpeed > World.SPEED_LIMIT) currentMaxSpeed = World.SPEED_LIMIT;
//				// if we're going too fast, and trying to go faster
//				if (currentSpeed >= currentMaxSpeed && speedChange > 0) {
//					// reduce our acceleration force in our current speed direction
//					float overSpeed = Vector3.Dot(direction, accelForce);
//					accelForce -= direction * overSpeed;
//				}
//				af.rb.velocity += accelForce * Time.deltaTime;
//			}
//			if(showDebugLines) {
//				Vector3 p = af.transform.position;
//				Vector3 v = p + af.rb.velocity;
//				LineRenderer lr = Lines.Make(ref line_velocity, Color.cyan, p, v, .1f, .1f);
//				lr.transform.SetParent (af.transform);
//				lr = Lines.Make(ref line_acceleration, Color.magenta, v, v + accelForce, .2f, 0);
//				lr.transform.SetParent (af.transform);
//			}
//		}
//		private GameObject line_velocity, line_acceleration, line_target, line_error;
//
//	}
//	/// <summary>
//	/// Dos the user actions.
//	/// </summary>
//	/// <returns><c>true</c>, if user actions was done, <c>false</c> otherwise.</returns>
//	/// <param name="t">T.</param>
//	public bool DoUserActions(Transform t) {
//		return false;
//	}
//
//	public Movement movement = new Movement();
//
//	public ResourceHolder GetResourceHolder() { if (!res) FindComponents (); return res; }
//	public MeshRenderer GetMeshRenderer() { if (!meshRend) FindComponents (); return meshRend; }
//	public Rigidbody GetRigidBody() { if (!rb) FindComponents (); return rb; }
//	public SphereCollider GetCollisionSphere() { return GetComponent<SphereCollider> (); }
//	public void SetUserSoul(AgentSoul soul) { this.soul = soul; }
//	public AgentSoul GetUserSoul() { return soul; }
//
//	private static T SearchSelfAndChildrenFor<T>(Transform t) where T : Component {
//		T found = t.GetComponent<T> ();
//		for (int i = 0; !found && i < t.childCount; ++i) {
//			found = SearchSelfAndChildrenFor<T> (t.GetChild (i));
//		}
//		return found;
//	}
//	// TODO merge with SearchSelfAndChildrenFor<T> somehow...
//	public static COMPONENT FindComponent<COMPONENT>(Transform t, bool parents, bool children) where COMPONENT : Component {
//		COMPONENT c = null;
//		do {
//			c = t.GetComponent<COMPONENT>();
//			if(!c && children) {
//				for (int i = 0; i < t.childCount; ++i) {
//					c = t.GetChild(i).GetComponent<COMPONENT>();
//					if (c) break;
//				}
//			}
//			t = (parents)?t.parent:null;
//		} while (!c && t);
//		return c;
//	}
//
//	private void FindComponents() {
//		meshRend = SearchSelfAndChildrenFor<MeshRenderer> (transform);
//		res = SearchSelfAndChildrenFor<ResourceHolder> (transform);
//		if(!rb) rb = GetComponent<Rigidbody>();
//	}
//
//	public void Rebirth() {
//        movement.aISettings.Reset();
//        if (!res) FindComponents ();
//		resetValues ();
//		GetCollisionSphere ().isTrigger = false;
//	}
//
//	void Start () {
//		FindComponents ();
//		rb.freezeRotation = true;
//		defaultVariables = GetResourceHolder ().resources.AsDictionary;
//		GetComponent<SphereCollider>().isTrigger = true;
//		halo = FindComponent<ParticleSystem>(transform, false, true);
//		preferredColor = currentColor;
//		SetColor(currentColor);
//		SetEffectsSize(0);
//		SetMass(this.mass);
//	}
//
//
//	void FixedUpdate() {
//		movement.TurnModelTowardCorrectDirection (soul, transform, Time.deltaTime);
//		if (IsAlive ()) {
//			movement.DoSteering (this);
//		}
//		movement.DoPhysics (this);
//	}
//
//
////    public void JustAcheivedObjective(GameObject objective)
////    {
////        // if target is reached, target self for a while...
////        if(movement.target && objective.transform == target.transform) {
////            // take a break for 1 to 10 seconds...
////            timer = Random.Range(0.5f, 2);
////            SetTarget(gameObject);
////            flee = true;
////        }
////    }
//
//	public const float MINIMUM_PREY_SIZE = 0.85f;
//
//	private Color currentColor = Color.white;
//	public Color preferredColor = Color.white;
//	public float effectsSize = 1, mass = 1, targetScale = 1;
//	private float scale = 1;
//	bool isAlive = true;
//
//	public ParticleSystem halo;
//	public Group team;
//
//	public float GetSize() { return effectsSize; }
//
//	public Group GetTeam() { return team; }
//	public void SetTeam(Group team) {
//		if (this.team) { this.team.RemoveMember(this); }
//		this.team = team;
//		if (this.team) { this.team.AddMember(this); }
//	}
//
//	public void SetAlive(bool alive){ this.isAlive = alive; }
//	public bool IsAlive() { return this.isAlive; }
//	public bool IsDead() { return !IsAlive (); }
//	public Color GetCurrentColor() { return currentColor; }
//
////	public PlayerForce GetPlayerForce(){ if(!pf) pf = FindComponent<PlayerForce>(transform, true, false); return pf;}
//
//	Dictionary<string,float> defaultVariables;
//
//	public void SetEffectsSize(float n) {
//		effectsSize = n;
//		if (halo) {
//			halo.Emit(1);
//			halo.startSize = effectsSize;
//			halo.Emit(1);
//			halo.Play();
//		}
//		TrailRenderer trail = FindComponent<TrailRenderer>(transform, false, true);
//		if (trail) trail.startWidth = effectsSize;
//	}
//
//
//	public static float ColorDistance(Color a, Color b) {
//		Vector3 v = new Vector3(a.r - b.r, a.g - b.g, a.b - b.b);
//		return v.magnitude;
//	}
//
//	public void resetValues() {
//		SetEffectsSize(0);
//		SetMass(1);
//		SetAlive (true);
//		GetResourceHolder ().resources.Copy (defaultVariables);
//		transform.position = Vector3.zero;//World.GetRandomLocation ();
//		int otherPeopleWithThisName;
//		do{
//			name = PlayerMaker.RandomName();
//			otherPeopleWithThisName = 0;
//		}while(otherPeopleWithThisName > 0);
//		SetTeam(Singleton.Get<GroupManager>().NewGroup());
//	}
//
//	public static void SetTrailRendererColor(TrailRenderer trail, Color c) { trail.material.SetColor("_TintColor", c); }
//	public static Color GetTrailRendererColor(TrailRenderer trail) { return trail.material.GetColor("_TintColor"); }
//
//	public void SetColor(Color color) {
//		this.currentColor = color;
//		if(halo) halo.startColor = color;
//		TrailRenderer trail = FindComponent<TrailRenderer>(transform, false, true);
//		if (trail) {
//			SetTrailRendererColor(trail, new Color(color.r, color.g, color.b, 0.25f));
//			Color c = color;
//			MeshRenderer r = GetMeshRenderer ();
//			c.a = r.materials [0].color.a;//playerObject.GetComponent<MeshRenderer>().materials[0].color.a;
//			r.materials[0].color = c;
//		}
//	}
//
//	public void SetMass(float n) {
//		this.mass = n;
//		targetScale = Mathf.Sqrt(n) * World.SIZE_MODIFIER;
//		Rigidbody rb = GetRigidBody ();
//		rb.mass = Mathf.Sqrt(this.mass) * World.MASS_MODIFIER;
//		// don't ever let the rigidbody-physics mass be zero. bad things happen if mass is zero.
//		if (rb.mass < 1) rb.mass = 1;
//		if (this.effectsSize < this.targetScale) {
//			SetEffectsSize(this.targetScale);
//		}
//		if (mass <= 0.125f) {
//			if (mass < 0) Debug.LogError("mass deficit for " + name);
//			Die();
//		}
//	}
//
//	public float GetMass() { return this.mass; }
//
//	public void Die() {
//		if (IsAlive()) {
//			SetAlive (false);
//			SetTeam(null);
//			AgentSoul soul = GetUserSoul ();
//			if (soul) { soul.Disconnect (this); }
//			GetCollisionSphere ().isTrigger = true;
//			SetMass(0);
//			GetRigidBody().velocity = Vector3.zero;
//			TrailRenderer trail = FindComponent<TrailRenderer>(transform, false, true);
//			float tailTime = trail.time;
//			int deathTimeOut = (int)(tailTime * 1000);
//			//print (name+" is now dead.");
//			//float originalMass = this.mass;
//			TimeMS.TimerCallback(deathTimeOut, () => {
//				//print (name+" is being cleared for respawn.");
//				Effects.ResetTrailRenderer(trail);
//				// reset the body just before release, so that when it is reborn, it has default values
//				resetValues();
//				if (soul) { soul.SetNeedsBody(true); }
//				MemoryPoolItem.Destroy(gameObject);
//			});
//		}
//	}
//
//	void Update() {
//		if (scale != targetScale) {
//			float dir = targetScale - scale;
//			float s = //(scaleVelocity * ((dir < 0) ? -1 : 1)) 
//				dir * Time.deltaTime;
//			scale += s;
//			if(scale > targetScale ^ dir < 0) {
//				scale = targetScale;
//			}
//			transform.localScale = new Vector3(scale, scale, scale);
//		}
//		if(this.currentColor != preferredColor) {
//			float d = ColorDistance(this.currentColor, preferredColor);
//			SetColor(Color.Lerp(this.currentColor, preferredColor, (1 - d) * Time.deltaTime));
//		}
//	}
//
//	public float GetAppropriateSizeOfEnergy() {
//		int count = (int)Mathf.Sqrt(mass);
//		if (count < 1) count = 1;
//		return mass / count;
//	}
//
}
