using UnityEngine;
using System.Collections;

public class ResourceEater : MonoBehaviour {

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
        resetValues();
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
				AddValue(e.mass);
                //e.AddValue(-e.mass);
                e.resetValues();
                MemoryPoolItem.Destroy(e.playerObject.gameObject);
            }
        }
	}
}
