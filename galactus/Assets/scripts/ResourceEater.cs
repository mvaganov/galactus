using UnityEngine;
using System.Collections;

public class ResourceEater : MonoBehaviour {
    public const float MINIMUM_PREY_SIZE = 0.85f;

    private Color currentColor = Color.white;
    public Color preferredColor = Color.white;
	public float effectsRadius = 1, mass = 1, targetScale = 1;
	private float scale = 1;
    bool isAlive = true;

    public PlayerForce pf;
    public ParticleSystem halo;
    public Team team;

    public float GetRadius() { return effectsRadius; }

    public Team GetTeam() { return team; }
    public void SetTeam(Team team) {
        if (this.team) { this.team.RemoveMember(pf); }
        this.team = team;
        if (this.team) { this.team.AddMember(pf); }
    }

	public void SetAlive(bool alive){ this.isAlive = alive; }
	public bool IsAlive() { return this.isAlive; }
	public bool IsDead() { return !IsAlive (); }
	public Color GetCurrentColor() { return currentColor; }

    public COMPONENT FindComponent<COMPONENT>(bool parents, bool children) where COMPONENT : Component {
        COMPONENT c = null;
        Transform t = transform;
        do {
            c = t.GetComponent<COMPONENT>();
            if(!c && children) {
                for (int i = 0; i < t.childCount; ++i) {
                    c = t.GetChild(i).GetComponent<COMPONENT>();
                    if (c) break;
                }
            }
            t = (parents)?t.parent:null;
        } while (!c && t);
        return c;
    }

	public PlayerForce GetPlayerForce(){ if(!pf) pf = FindComponent<PlayerForce>(true, false); return pf;}

	void Start() {
        GetComponent<SphereCollider>().isTrigger = true;
        halo = FindComponent<ParticleSystem>(false, true);
        preferredColor = currentColor;
        SetColor(currentColor);
        SetEffectsSize(0);
        SetMass(this.mass);
    }

    void Update() {
        if (scale != targetScale) {
            float dir = targetScale - scale;
            float s = //(scaleVelocity * ((dir < 0) ? -1 : 1)) 
                dir * Time.deltaTime;
            scale += s;
            if(scale > targetScale ^ dir < 0) {
                scale = targetScale;
            }
            pf.transform.localScale = new Vector3(scale, scale, scale);
        }
        if(this.currentColor != preferredColor) {
            float d = ColorDistance(this.currentColor, preferredColor);
            SetColor(Color.Lerp(this.currentColor, preferredColor, (1 - d) * Time.deltaTime));
        }
    }

    public float GetAppropriateSizeOfEnergy() {
        int count = (int)Mathf.Sqrt(mass);
        if (count < 1) count = 1;
        return mass / count;
    }

    // TODO create a new component for shooting?
    private float shootCooldown = 0;
    public bool DoUserActions(Transform direction) {
        if (shootCooldown > 0) shootCooldown -= Time.deltaTime;
        if ((Input.GetButton("Fire1") || Input.GetKey(KeyCode.X)) && shootCooldown <= 0) {
            // shoot hostile resource
            Vector3 dir = direction.forward;
            //if (pf.GetUserSoul()) { dir = pf.GetUserSoul().GetLookTransform().forward; }
            EjectOne(dir, -GetRadius(), null, 0, GetRadius());
            shootCooldown = .25f;
            ChangeMass(-GetRadius() * World.DAMAGE_ENERGY_COST_RATIO);
            return true;
        } else if (Input.GetButtonDown("Fire2")) {
            // release resources on your own
            EjectOne(direction.forward, GetAppropriateSizeOfEnergy(), this, 0, -1);
            return true;
        } else if (Input.GetKeyDown(KeyCode.P)) {
            float newMass = GetMass() / 2;
            SetMass(newMass);
            World w = World.GetInstance();
            ResourceMaker rm = w.spawner;
            GameObject dup = Instantiate(pf.gameObject) as GameObject;
            PlayerForce dupPf = dup.GetComponent<PlayerForce>();
            dupPf.GetResourceEater().scale = 0;
            float superTiny = 1 / 1024f;
            dup.transform.localScale = new Vector3(superTiny, superTiny, superTiny);
            UserSoul soul = pf.GetUserSoul();
            Vector3 birthPoint = transform.position + soul.cameraTransform.forward * GetRadius();
            dup.transform.position = birthPoint;
            rm.ResourcePoof(birthPoint, GetCurrentColor(), newMass);
            soul.Posess(dupPf, false);
            return true;
        } else if (Input.GetKeyDown(KeyCode.F9)) {
            ChangeMass(10);
            return true;
        }
        return false;
    }

    public static float ColorDistance(Color a, Color b) {
        Vector3 v = new Vector3(a.r - b.r, a.g - b.g, a.b - b.b);
        return v.magnitude;
    }

    public void resetValues() {
        SetEffectsSize(0);
        SetMass(1);
		SetAlive (true);
		pf.transform.position = Vector3.zero;//World.GetRandomLocation ();
		name = PlayerMaker.RandomName();
        SetTeam(Team.NewTeam());
    }

    public static void SetTrailRendererColor(TrailRenderer trail, Color c) { trail.material.SetColor("_TintColor", c); }
    public static Color GetTrailRendererColor(TrailRenderer trail) { return trail.material.GetColor("_TintColor"); }

    public void SetColor(Color color) {
        this.currentColor = color;
        if(halo) halo.startColor = color;
        TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
        if (trail) {
            SetTrailRendererColor(trail, new Color(color.r, color.g, color.b, 0.25f));
            Color c = color;
			MeshRenderer r = GetPlayerForce().GetMeshRenderer ();
			c.a = r.materials [0].color.a;//playerObject.GetComponent<MeshRenderer>().materials[0].color.a;
            r.materials[0].color = c;
        }
    }

    public void SetMass(float n) {
        this.mass = n;
        targetScale = Mathf.Sqrt(n) * World.SIZE_MODIFIER;
		Rigidbody rb = GetPlayerForce().GetRigidBody ();
        rb.mass = Mathf.Sqrt(this.mass) * World.MASS_MODIFIER;
		// don't ever let the rigidbody-physics mass be zero. bad things happen if mass is zero.
		if (rb.mass < 1) rb.mass = 1;
        if (this.effectsRadius < this.targetScale) {
            SetEffectsSize(this.targetScale);
        }
        if (mass <= 0.125f) {
            if (mass < 0) Debug.LogError("mass deficit for " + name);
            Die();
        }
    }

    public float GetMass() { return this.mass; }

    public void Die() {
		if (IsAlive()) {
			SetAlive (false);
            SetTeam(null);
            UserSoul soul = GetPlayerForce().GetUserSoul ();
			if (soul) { soul.Disconnect (pf); }
			pf.GetCollisionSphere ().isTrigger = true;
			SetMass(0);
			pf.GetRigidBody().velocity = Vector3.zero;
            TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
            float tailTime = trail.time;
            int deathTimeOut = (int)(tailTime * 1000);
			//print (name+" is now dead.");
            //float originalMass = this.mass;
			TimeMS.TimerCallback(deathTimeOut, () => {
				//print (name+" is being cleared for respawn.");
                World.ResetTrailRenderer(trail);
                // reset the body just before release, so that when it is reborn, it has default values
                resetValues();
                if (soul) { soul.SetNeedsBody(true); }
				MemoryPoolItem.Destroy(pf.gameObject);
            });
        }
    }

    public void SetEffectsSize(float n) {
        effectsRadius = n;
        if (halo) {
            halo.Emit(1);
            halo.startSize = effectsRadius;
            halo.Emit(1);
            halo.Play();
        }
        TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
        if (trail) trail.startWidth = effectsRadius;
    }

    public void ChangeMass(float delta) { SetMass(this.mass + delta); }

    private void AddValue(float v) { ChangeMass(v); }

    public void EatResource(ResourceNode resource) {
        float value = resource.GetValue();
        Color color = resource.GetColor();
        if (value > 0) {
            AddValue(value);
            GetPlayerForce().JustAcheivedObjective(resource.gameObject);
            preferredColor = color;
        } else if(value < 0) {
            Vector3 direction = Vector3.zero;
            Rigidbody rb = resource.GetComponent<Rigidbody>();
            if (rb && rb.velocity != Vector3.zero) { direction = rb.velocity.normalized; }
            if (direction == Vector3.zero) {
                direction = transform.position - resource.transform.position;
                if (direction != Vector3.zero) { direction = rb.velocity.normalized; }
            }
            if (direction == Vector3.zero) { direction = Random.onUnitSphere; }
            EjectOne(direction, -value, this, -value, value);
        }
        //resource.SetValue(0);
        MemoryPoolItem.Destroy(resource.gameObject);
    }

    void OnTriggerEnter(Collider c) {
		// could be colliding with a ResourceEater or a PlayerForce. get the ResourceEater in either case.
		ResourceEater e = c.gameObject.GetComponent<ResourceEater> ();
		if (!e) {
			PlayerForce pf = c.gameObject.GetComponent<PlayerForce> ();
			if (pf) {
				e = pf.GetResourceEater ();
			}
		}
		Attack(e);
	}

   public bool IsOnSameTeam(ResourceEater e) { return (e.team && e.team == team); }

	void Attack(ResourceEater e) {
		if(e == null || e == this) { return; }
		if (e.mass >= 0 && !IsOnSameTeam(e) && e.mass < (mass * MINIMUM_PREY_SIZE)) {
			float distance = Vector3.Distance (e.transform.position, transform.position);
			if (distance < transform.lossyScale.x) {
                //print(name + " attacks " + e.name);
                int count = (int)Mathf.Sqrt(e.mass);
                if (count < 1) count = 1;
				e.Eject (false, count, e.mass / count, this, 1, -1);
			}
		} else {
			if(mass < (e.mass * MINIMUM_PREY_SIZE)) e.Attack (this);
		}
	}

    // TODO do these variables still make sense?
    public const float minimumTransientEnergySeconds = 10, secondsIncreasePerRelease = 1;

    public void Eject(bool forward, int howMany, float amountPerEjection, ResourceEater target, float edibleDelay, float lifetimeInSeconds) {
        Vector3 dir = transform.forward;
        if(howMany == 1) {
            if (!forward) dir = Random.onUnitSphere;
            EjectOne(dir, amountPerEjection, target, edibleDelay, lifetimeInSeconds);
        } else for (int i = 0; i < howMany; ++i) {
            TimeMS.TimerCallback(i * 100, () => {
                if (!forward) dir = Random.onUnitSphere;
                EjectOne(dir, amountPerEjection, target, edibleDelay, lifetimeInSeconds);
            });
        }
    }

    public ResourceNode EjectOne(Vector3 direction, float size, ResourceEater target, float edibleDelayInSeconds, float lifetimeInSeconds) {
        if (mass <= 0) return null;
        if (size > 0) {
            if (mass <= size) size = mass;
            ChangeMass(-size);
        }
		GetPlayerForce ();
        World w = World.GetInstance();
        TrailRenderer mommaTrail = FindComponent<TrailRenderer>(false, true);
        ResourceNode n = w.spawner.CreateResourceNode(transform.position + direction * effectsRadius, size, currentColor);
        n.transform.rotation = Quaternion.LookRotation(direction);
        Rigidbody rb = n.gameObject.GetComponent<Rigidbody>();
        if (!rb) { rb = n.gameObject.AddComponent<Rigidbody>(); rb.useGravity = false; }
        TrailRenderer tr = n.gameObject.GetComponent<TrailRenderer>();
        SetTrailRendererColor(tr, GetTrailRendererColor(mommaTrail));
        tr.startWidth = effectsRadius * 0.0625f;
        tr.endWidth = 0;
        tr.time = 0.125f;
        ResourceOrbit s = n.gameObject.AddComponent<ResourceOrbit>();
        if (size > 0) {
            s.Setup(target, pf.maxSpeed * 2.5f, pf.maxSpeed * 8);
        } else {
            s.Setup(target, World.PROJECTILE_SPEED_LIMIT, 0);
        }
        rb.velocity = n.transform.forward * pf.maxSpeed;
        n.creator = this;
		if (target == this) {
            int msDelay = (int)(minimumTransientEnergySeconds * 1000 + (numReleases * secondsIncreasePerRelease * 1000));
            numReleases++;
			// make sure this ejected resource node takes away from the num released
			MemoryPoolRelease.Add (n.gameObject, (obj) => {
				if (n.creator){ numReleases -= 1; }
				n.creator = null;
			});
			// after a specific amount of time, this resource node is retreivable.
			TimeMS.TimerCallback (msDelay, () => {
				if (n.creator){ numReleases -= 1; }
				n.creator = null;
			});
		}
        if(edibleDelayInSeconds > 0) {
            n.SetEdible(false);
            TimeMS.TimerCallback((int)(edibleDelayInSeconds * 1000), () => { n.SetEdible(true); });
        }
        if (lifetimeInSeconds <= 0) lifetimeInSeconds = World.MAX_RESOURCE_LIFETIME_IN_SECONDS;
        n.SetLifetime(lifetimeInSeconds);
        return n;
    }
	/// <summary>The number of energy packets floating around this entity</summary>
	int numReleases;

}
