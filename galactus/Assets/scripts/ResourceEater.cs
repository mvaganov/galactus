using UnityEngine;
using System.Collections;

public class ResourceEater : MonoBehaviour {

    public Color preferredColor = Color.white;
	public float effectsRadius = 1, mass = 1, targetScale = 1;
	private float scale = 1;
	private Color currentColor = Color.white;
	/// <summary>how fast rescale happens when size changes</summary>
	public const float scaleVelocity = 1;
	public const float minimumPreySize = 0.85f;

	public PlayerForce pf;
    public ParticleSystem halo;

    public float GetRadius() { return effectsRadius; }

	bool isAlive = true;

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

	PlayerForce GetPlayerForce(){ if(!pf) pf = FindComponent<PlayerForce>(true, false); return pf;}

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
            float s = (scaleVelocity * ((dir < 0) ? -1 : 1)) * Time.deltaTime;
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

    float ColorDistance(Color a, Color b) {
        Vector3 v = new Vector3(a.r - b.r, a.g - b.g, a.b - b.b);
        return v.magnitude;
    }

    public void resetValues() {
        SetEffectsSize(0);
        SetMass(1);
		SetAlive (true);
		pf.transform.position = Vector3.zero;//World.GetRandomLocation ();
		name = PlayerMaker.RandomName();
    }

    public void SetColor(Color color) {
        this.currentColor = color;
        if(halo) halo.startColor = color;
        TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
        if (trail) {
            Color slighter = new Color(color.r, color.g, color.b, 0.25f);
            trail.material.SetColor("_TintColor", slighter);
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
			RespawningPlayer soul = GetPlayerForce().GetUserSoul ();
			if (soul) { soul.Disconnect (); }
			pf.GetCollisionSphere ().isTrigger = true;
			SetMass(0);
			pf.GetRigidBody().velocity = Vector3.zero;
            TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
            float tailTime = trail.time;
            int deathTimeOut = (int)(tailTime * 1000);
			print (name+" is now dead.");
            //float originalMass = this.mass;
			TimeMS.TimerCallback(deathTimeOut, () => {
				print (name+" is being cleared for respawn.");
                World.ResetTrailRenderer(trail);
                // reset the body just before release, so that when it is reborn, it has default values
                resetValues();
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

    public void EatResource(float value, Color color) {
        AddValue(value);
		GetPlayerForce().ClearTarget();
        preferredColor = color;
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

	void Attack(ResourceEater e) {
		if(e == null || e == this) { return; }
		if (e.mass >= 0 && e.mass < (mass * minimumPreySize)) {
			float distance = Vector3.Distance (e.transform.position, transform.position);
			if (distance < transform.lossyScale.x) {
				//print(name + " attacks " + e.name);
				e.Eject (false, e.mass, transform, 1);
			}
		} else {
			if(mass < (e.mass * minimumPreySize)) e.Attack (this);
		}
	}

    public static int countReleasesPerSprint = 5;
    public static float minimumTransientEnergySeconds = 10;
    public static float secondsIncreasePerRelease = 1;

    public void Eject(bool forward, float amount, Transform target, float edibleDelay) {
        Vector3 dir = transform.forward;
        float amountPerPacket = amount / countReleasesPerSprint;
        for (int i = 0; i < countReleasesPerSprint; ++i) {
            TimeMS.TimerCallback(i * 100, () => {
                if (!forward) dir = Random.onUnitSphere;
                EjectOne(dir, amountPerPacket, target, edibleDelay);

            });
        }
    }

    bool EjectOne(Vector3 direction, float size, Transform target, float edibleDelay) {
        if (mass <= 0) return false;
        if (mass <= size) size = mass;
        ChangeMass(-size);
		GetPlayerForce ();
        World w = World.GetInstance();
        TrailRenderer mommaTrail = FindComponent<TrailRenderer>(false, true);
        ResourceNode n = w.spawner.CreateResourceNode(transform.position + direction * effectsRadius, size, currentColor);
        Rigidbody rb = n.gameObject.GetComponent<Rigidbody>();
        if (!rb) { rb = n.gameObject.AddComponent<Rigidbody>(); }
        TrailRenderer tr = n.gameObject.GetComponent<TrailRenderer>();
        tr.material = mommaTrail.material;
        tr.startWidth = effectsRadius * 0.0625f;
        tr.endWidth = 0;
        tr.time = 0.125f;
        SimpleGravityForce s = n.gameObject.AddComponent<SimpleGravityForce>();
        s.Setup(target, pf.maxSpeed * 2.5f, pf.maxSpeed * 8);
        rb.velocity = n.transform.forward * pf.maxSpeed;
        n.creator = gameObject;
        int msDelay = (int)(minimumTransientEnergySeconds * 1000 + (numReleases * secondsIncreasePerRelease * 1000));
		if (target == transform || target == pf.transform) {
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
        if(edibleDelay > 0) {
            n.SetEdible(false);
            TimeMS.TimerCallback((int)(edibleDelay * 1000), () => { n.SetEdible(true); });
        }
        return true;
    }
	/// <summary>The number of energy packets floating around this entity</summary>
	int numReleases;

}
