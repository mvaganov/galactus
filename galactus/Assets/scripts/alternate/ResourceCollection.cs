using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResourceCollection : MonoBehaviour {

	MemoryPool<GameObject> resourceNodes;
	public Agent_Properties pfab_resource;
	public string resourceName = "energy";
	private Effects.Effect harvest, waste;

    struct Suppression {
		public Vector3 position;
		public float expiration; // TODO use unix time in MS.
		public Suppression(Vector3 position, float expiration) { this.position = position; this.expiration = expiration; }
	}
	// TODO add some kind of spatial data structure, like an Octree, for the Supression objects
	Queue<Suppression> suppression = new Queue<Suppression>();

	public void AddSuppression(Vector3 loc) {
		waste.ps.SetParticleLifetime (resourceSettings.suppressionDuration);
		waste.ps.SetParticleSize(resourceSettings.suppressionRange);
		waste.Emit (5, loc, null);
		suppression.Enqueue(new Suppression(loc, Time.time + resourceSettings.suppressionDuration));
	}

	// TODO move this to the GameRules object
	[System.Serializable]
	public class Settings {
		public int maxActive = 100;
		public float creationDelay = 1;
		public float suppressionDuration = 60;
		public float suppressionRange = 30;
		public float minValue = 1, maxValue = 10;
	}

	public Settings resourceSettings = new Settings();

	void Start() {
		GameRules rules = Singleton.Get<GameRules> ();
		rules.RegisterResourceHolderPrefab (pfab_resource.gameObject);
		resourceNodes = new MemoryPool<GameObject>();
		resourceNodes.Setup(
			() => Instantiate(pfab_resource.gameObject),
			(obj) => { obj.SetActive(true); },
			(obj) => {
				Agent_SizeAndEffects n = obj.GetComponent<Agent_SizeAndEffects>();
				Harvest(n);
                bool moving = false;
                Rigidbody rb = obj.GetComponent<Rigidbody>();
				if (rb) { moving = (rb.velocity != Vector3.zero); rb.velocity = Vector3.zero; }
                obj.SetActive(false);
				if(!moving)
    				AddSuppression(obj.transform.position);
				//ResourceCollectable n = obj.GetComponent<ResourceCollectable>();
				Effects.ResetTrailRenderer(obj.GetComponent<TrailRenderer>());
            },
			(obj) => Object.Destroy(obj)
		);
		Effects e = Singleton.Get<Effects> ();
		waste = e.Get ("waste");
		harvest = e.Get ("harvest");
	}

	public void Harvest(Agent_SizeAndEffects rn) {
		float v = rn.GetEnergy();
		// if this is the energy nodes last moment...
		if (v == 0) { 
			// account for every part of it
			ParticleSystem ps = rn.GetComponent<ParticleSystem> ();
			int count = ps.particleCount;
			ParticleSystem.Particle[] parts = new ParticleSystem.Particle[count];
			ps.GetParticles (parts);
			harvest.ps.SetColor (rn.GetColor ());
			harvest.ps.SetParticleSize (count);
			harvest.ps.SetParticleSpeed (count);
			for (int i = 0; i < parts.Length; ++i) {
				harvest.Emit (1, parts [i].position, null);
			}
		}
        else if(v > 0) {
            ResourcePoof(rn.transform.position, rn.GetColor(), v);
        }
    }

    public void ResourcePoof(Vector3 position, Color color, float size) {
		print (size);
		harvest.ps.SetColor (color);
		harvest.ps.SetParticleSize(size);
		harvest.ps.SetParticleSpeed(size);
		harvest.Emit ((int)(Mathf.Sqrt (size) * 10), position, null);
    }

    public bool IsBlocked(Vector3 testLoc, float suppressionRange) {
		foreach(Suppression s in suppression) {
			float dist = (testLoc - s.position).magnitude;
			if(dist < suppressionRange) return true;
		}
		return false;
	}

	public Agent_SizeAndEffects CreateRandomResourceNode() {
		Vector3 loc = Vector3.zero;
		bool supressed = false;
		int iterations = 0;
		GameRules gr = Singleton.Get<GameRules> ();
		do {
			loc = gr.GetRandomLocation();
			supressed = IsBlocked(loc, resourceSettings.suppressionRange);
			iterations++;
			if(iterations > 10) break;
		} while(supressed);
        return CreateResourceNode(loc,
            Random.Range(resourceSettings.minValue, resourceSettings.maxValue),
			new Color(Random.Range(0, 1.0f), Random.Range(0, 1.0f), Random.Range(0, 1.0f)), resourceName);
	}

	public Agent_SizeAndEffects CreateResourceNode(Vector3 loc, float value, Color color, string resourceName) {
		Agent_SizeAndEffects rn = resourceNodes.Alloc().GetComponent<Agent_SizeAndEffects>();
		if (rn.name.EndsWith ("(Clone)")) {
			rn.name = rn.name.Substring (0, rn.name.Length - "(Clone)".Length);
		}
		int incarnationDelimeterIndex = rn.name.LastIndexOf ("#");
		if (incarnationDelimeterIndex >= 0) {
			rn.name = rn.name.Substring (0, incarnationDelimeterIndex);
		}
		rn.name += "#"+rn.GetInstanceID()+"."+ MemoryPoolItem.GetIncarnation (rn.gameObject);
		Agent_Properties h = rn.GetComponent<Agent_Properties> ();
		h.EnsureComponents ();
		h.SetValue (resourceName, value);
        rn.SetColor(color);
        rn.transform.position = loc;
//		rn.SetSize(value);
        rn.transform.parent = transform;
        return rn;
    }

    void FixedUpdate() {
		while(suppression.Count > 0 && suppression.Peek().expiration < Time.time) {
			suppression.Dequeue();
		}
	}

	float timer = 0;
	void Update () {
		if(timer < resourceSettings.creationDelay) {
			timer += Time.deltaTime;
		}
		if(timer >= resourceSettings.creationDelay) {
			if(resourceNodes.Count() < resourceSettings.maxActive) {
				CreateRandomResourceNode();
				timer -= resourceSettings.creationDelay;
			} else {
				timer = resourceSettings.creationDelay;
			}
		}
	}
}
