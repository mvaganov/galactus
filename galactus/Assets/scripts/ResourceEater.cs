using UnityEngine;
using System.Collections;

public class ResourceEater : MonoBehaviour {

    // TODO interpolate scale when increasing in size

    public Color color = Color.white;
	public GameObject playerObject;
	public World w;
    public float effectsRadius = 1, mass = 1, scale = 1, targetScale = 1, scaleVelocity = 1;

	//float currentAttackPower = 0;

	//private static Vector3 one = new Vector3(.1f, .1f, .1f);
    public ParticleSystem halo;

    public float GetRadius() { return effectsRadius; }

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
        SetEffectsSize(0);
        SetMass(this.mass);
    }

    void Update()
    {
        if (scale != targetScale)
        {
            float dir = targetScale - scale;
            float s = (scaleVelocity * ((dir < 0) ? -1 : 1)) * Time.deltaTime;
            scale += s;
            if(scale > targetScale ^ dir < 0)
            {
                scale = targetScale;
            }
            playerObject.transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    public void resetValues()
    {
        // division to undo the MASS_MODIFIER done every time mass is set.
		SetMass(1);
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
        targetScale = Mathf.Sqrt(n) * World.SIZE_MODIFIER;
        if(playerObject == null)
        {
            PlayerForce pf = FindComponent<PlayerForce>(true, false);
            playerObject = pf.gameObject;
        }
        //print(n);
        playerObject.GetComponent<Rigidbody>().mass = Mathf.Sqrt(this.mass) * World.MASS_MODIFIER;
        if (this.effectsRadius < this.targetScale)
        {
            SetEffectsSize(this.targetScale);
        }
        if (mass <= 0)
        {
            MemoryPoolItem.Destroy(playerObject.gameObject);
        }
        if (targetScale < 0.1f)
        {
            playerObject.GetComponent<MouseLook>().enabled = false;
        }
    }

    public void SetEffectsSize(float n)
    {
        effectsRadius = n;
        if (halo) halo.transform.localScale = new Vector3(effectsRadius, effectsRadius, effectsRadius);
        TrailRenderer trail = FindComponent<TrailRenderer>(false, true);
        if (trail) trail.startWidth = effectsRadius;
    }

    public void ChangeMass(float delta) {
        SetMass(this.mass + delta);
    }

    public void AddValue(float v) {
        ChangeMass(v);
    }

    void OnTriggerEnter(Collider c) {
		Attack(c.gameObject.GetComponent<ResourceEater>());
	}

	void Attack(ResourceEater e) {
		if(e == null) { return; }
		if(e.mass >= 0 && e.mass< (mass * 0.85f)) {
			float distance = Vector3.Distance(e.transform.position, transform.position);
			if(distance < transform.lossyScale.x) {
                print(name + " attacks " + e.name);
                //AddValue(e.mass);
                e.Eject(false, e.mass, transform, 1);
                //e.AddValue(-e.mass);
                //e.transform.parent.GetComponent<MouseLook>().enabled = false;
                //MemoryPoolItem.Destroy(e.playerObject.gameObject);
            }
        }
	}

    public static int countReleasesPerSprint = 5;
    public static float minimumTransientEnergySeconds = 10;
    public static float secondsIncreasePerRelease = 1;

    int numReleases;

    public void Eject(bool forward, float amount, Transform target, float edibleDelay)
    {
        Vector3 dir = transform.forward;
        float amountPerPacket = amount / countReleasesPerSprint;
        for (int i = 0; i < countReleasesPerSprint; ++i)
        {
            TimeMS.TimerCallback(i * 100, () => {
                if (!forward) dir = Random.onUnitSphere;
                if (EjectOne(dir, amountPerPacket, target, edibleDelay))
                    if (mass <= amountPerPacket) amountPerPacket = mass;
                    ChangeMass(-amountPerPacket);
            });
        }
    }

    bool EjectOne(Vector3 direction, float size, Transform target, float edibleDelay)
    {
        PlayerForce pf = FindComponent<PlayerForce>(true, false);
        Quaternion r = pf.transform.rotation;
        r.SetLookRotation(direction);
        World w = World.GetInstance();
        TrailRenderer mommaTrail = FindComponent<TrailRenderer>(false, true);
        ResourceNode n = w.spawner.CreateResourceNode(transform.position + direction * effectsRadius, size, color);
        Rigidbody rb = n.gameObject.GetComponent<Rigidbody>();
        if (!rb) { rb = n.gameObject.AddComponent<Rigidbody>(); }
        TrailRenderer tr = n.gameObject.GetComponent<TrailRenderer>();
        tr.material = mommaTrail.material;
        tr.startWidth = effectsRadius * 0.0625f;
        tr.endWidth = 0;
        tr.time = 0.125f;
        Seeker s = n.gameObject.AddComponent<Seeker>();
        s.Setup(target, pf.maxSpeed * 2.5f, pf.maxSpeed * 8);
        rb.velocity = n.transform.forward * pf.maxSpeed;
        n.creator = gameObject;
        int msDelay = (int)(minimumTransientEnergySeconds * 1000 + (numReleases * secondsIncreasePerRelease * 1000));
        //print(msDelay);
        TimeMS.TimerCallback(msDelay, () => {
            if (n.creator) // FIXME make a better way to determine if this energy particle thing was already resolved...
            {
                ReduceOrbitCount(1);
            }
            n.creator = null;
        });
        numReleases++;
        if(edibleDelay > 0)
        {
            n.SetEdible(false);
            TimeMS.TimerCallback((int)(edibleDelay * 1000), () => { n.SetEdible(true); });
        }
        return true;
    }

    public void ReduceOrbitCount(int n) { numReleases -= n; }

}
