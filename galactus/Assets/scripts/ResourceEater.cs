using UnityEngine;
using System.Collections;

public class ResourceEater : MonoBehaviour {

    // TODO interpolate scale when increasing in size

    public Color color = Color.white;
	public float score = 0;
	public GameObject playerObject;
	public World w;
    public float size = 1, mass = 1, radius = 1;

	//float currentAttackPower = 0;

	//private static Vector3 one = new Vector3(.1f, .1f, .1f);
    public ParticleSystem halo;

    public float GetRadius() { return radius; }

    public COMPONENT FindComponent<COMPONENT>(bool parents, bool children) where COMPONENT : Component {
        COMPONENT c = null;
        Transform t = transform;
        do {
            c = t.GetComponent<COMPONENT>();
            if(!c && children)
            {
                for (int i = 0; i < t.childCount; ++i)
                {
                    c = t.GetChild(i).GetComponent<COMPONENT>();
                    if (c) break;
                }
            }
            t = (parents)?t.parent:null;
        } while (!c && t);
        return c;
    }

	void Start() {
        PlayerForce pf = FindComponent<PlayerForce>(true, false);
        playerObject = pf.gameObject;
		GetComponent<SphereCollider>().isTrigger = true;
        halo = FindComponent<ParticleSystem>(false, true);
        SetColor(color);
        SetSize(this.size);
        SetMass(this.mass);
    }

    public void resetValues()
    {
        score = 0;
        SetSize(1);
		SetMass(1 / World.MASS_MODIFIER);
    }

    public void SetColor(Color color)
    {
        this.color = color;
        if(halo) halo.startColor = color;
        TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
        if (trail)
        {
            Color slighter = new Color(color.r, color.g, color.b, 0.25f);
            trail.material.SetColor("_TintColor", slighter);
        }
    }

    public void SetMass(float n)
    {
        this.mass = n;
        float s = Mathf.Sqrt(n) * World.SIZE_MODIFIER;
        if(playerObject == null)
        {
            PlayerForce pf = FindComponent<PlayerForce>(true, false);
            playerObject = pf.gameObject;
        }
        playerObject.transform.localScale = new Vector3(s, s, s);
        playerObject.GetComponent<Rigidbody>().mass = Mathf.Sqrt(this.mass) * World.MASS_MODIFIER;
    }

    public void SetSize(float n)
    {
        this.size = n;
        float s = Mathf.Sqrt(n) * World.SIZE_MODIFIER;
        radius = s;
        if (halo) halo.transform.localScale = new Vector3(s, s, s);
        TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
        if (trail) trail.startWidth = s;
    }

    public void ChangeMass(float delta) { SetMass(this.mass + delta); }
    public void ChangeSize(float delta) {
        if (this.mass + delta > this.size)
        {
            SetSize(this.size + delta);
        }
    }

    public void AddValue(float v) {
		score += v;
		if(score <= 0) {
			print(playerObject.name + " should be dead!");
		} else {
            ChangeSize(v);
            ChangeMass(v);
		}
    }

    void OnTriggerEnter(Collider c) {
		Attack(c.gameObject.GetComponent<ResourceEater>());
	}

	void Attack(ResourceEater e) {
		if(e == null) { return; }
		//print(playerObject.name + " attacks " + e.playerObject.name);
		// TODO score * 0.9 could be in a variable called 'minimumEatableSize' or something
		if(e.mass >= 0 && e.mass< (mass * 0.85f)) {
			float distance = Vector3.Distance(e.transform.position, transform.position);
			if(distance < transform.lossyScale.x) {
                //AddValue(e.mass);
                e.Eject(false, e.mass, transform); // TODO make the ejection more explosive... like, prevent immidiate absorption, or blast out at high speed away from the player slightly...
                //e.AddValue(-e.mass);
                e.transform.parent.GetComponent<MouseLook>().enabled = false;
                MemoryPoolItem.Destroy(e.playerObject.gameObject);
            }
        }
	}

    public static int countReleasesPerSprint = 5;
    public static float minimumTransientEnergySeconds = 10;
    public static float secondsIncreasePerRelease = 1;

    int numReleases;

    public void Eject(bool forward, float amount, Transform target)
    {
        Vector3 dir = transform.forward;
        float amountPerPacket = amount / countReleasesPerSprint;
        for (int i = 0; i < countReleasesPerSprint; ++i)
        {
            TimeMS.TimerCallback(i * 100, () => {
                if (!forward) dir = Random.onUnitSphere;
                if (mass >= amountPerPacket && EjectOne(dir, amountPerPacket, target))
                    ChangeMass(-amountPerPacket);
            });
        }
    }

    bool EjectOne(Vector3 direction, float size, Transform target)
    {
        PlayerForce pf = FindComponent<PlayerForce>(true, false);
        Quaternion r = pf.transform.rotation;
        r.SetLookRotation(direction);
        World w = World.GetInstance();
        TrailRenderer mommaTrail = FindComponent<TrailRenderer>(false, true);
        ResourceNode n = w.spawner.CreateResourceNode(transform.position + Random.insideUnitSphere * radius, size, color);
        Rigidbody rb = n.gameObject.GetComponent<Rigidbody>();
        if (!rb) { rb = n.gameObject.AddComponent<Rigidbody>(); }
        TrailRenderer tr = n.gameObject.GetComponent<TrailRenderer>();
        tr.material = mommaTrail.material;
        tr.startWidth = radius * 0.0625f;
        tr.endWidth = 0;
        tr.time = 0.125f;
        Seeker s = n.gameObject.AddComponent<Seeker>();
        s.Setup(target, pf.maxSpeed * 2.5f, pf.maxSpeed * 8);
        rb.velocity = n.transform.forward * pf.maxSpeed;
        n.creator = gameObject;
        int msDelay = (int)(minimumTransientEnergySeconds * 1000 + (numReleases * secondsIncreasePerRelease * 1000));
        print(msDelay);
        TimeMS.TimerCallback(msDelay, () => {
            if (n.creator) // FIXME make a better way to determine if this energy particle thing was already resolved...
            {
                ReduceOrbitCount(1);
            }
            n.creator = null;
        });
        numReleases++;
        return true;
    }

    public void ReduceOrbitCount(int n) { numReleases -= n; print(numReleases); }

}
