using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResourceMaker : MonoBehaviour {

	MemoryPool<GameObject> resourceNodes;

	public ResourceNode resourceNode_prefab;
	public ParticleSystem resourceNodeHarvest;
	public ParticleSystem resourceSuppressVisual;

    struct Suppression {
		public Vector3 position;
		public float expiration;
		public Suppression(Vector3 position, float expiration) { this.position = position; this.expiration = expiration; }
	}
	Queue<Suppression> suppression = new Queue<Suppression>();

	public void AddSuppression(Vector3 loc) {
		resourceSuppressVisual.transform.position = loc;
		resourceSuppressVisual.startLifetime = resourceSettings.suppressionDuration;
		resourceSuppressVisual.startSize = resourceSettings.suppressionRange;
		resourceSuppressVisual.Emit(5);
		suppression.Enqueue(new Suppression(loc, Time.time + resourceSettings.suppressionDuration));
	}

	[System.Serializable]
	public class Settings {
		public int maxActive = 100;
		public float creationDelay = 1;
		public float suppressionDuration = 30;
		public float suppressionRange = 10;
		public float minValue = 1, maxValue = 10;
	}

	public Settings resourceSettings = new Settings();

	float timer = 0;
	int activeResources = 0;

	void Start() {
		resourceNodes = new MemoryPool<GameObject>();
		resourceNodes.Setup(
			() => Instantiate(resourceNode_prefab.gameObject),
			(obj) => { obj.SetActive(true); activeResources++; },
			(obj) => {
                bool moving = false;
                Rigidbody rb = obj.GetComponent<Rigidbody>();
				if (rb) { moving = rb.velocity != Vector3.zero; rb.velocity = Vector3.zero; }
                SimpleGravityForce s = obj.GetComponent<SimpleGravityForce>();
                if (s) Destroy(s);
                obj.SetActive(false);
				activeResources--;
				if(!moving)
    				AddSuppression(obj.transform.position);
                ResourceNode n = obj.GetComponent<ResourceNode>();
                Harvest(n);
                World.ResetTrailRenderer(obj.GetComponent<TrailRenderer>());
            },
			(obj) => Object.Destroy(obj)
		);
	}

	public void Harvest(ResourceNode rn) {
		resourceNodeHarvest.transform.position = rn.transform.position;
		float v = rn.GetValue();
		resourceNodeHarvest.startColor = rn.GetColor();
		resourceNodeHarvest.startSize = v;
		resourceNodeHarvest.startSpeed = v;
		resourceNodeHarvest.Emit((int)(Mathf.Sqrt(v) * 10));
	}

	public bool IsBlocked(Vector3 testLoc, float suppressionRange) {
		foreach(Suppression s in suppression) {
			float dist = (testLoc - s.position).magnitude;
			if(dist < suppressionRange) return true;
		}
		return false;
	}

	public ResourceNode CreateRandomResourceNode() {
		Vector3 loc = Vector3.zero;
		bool supressed = false;
		int iterations = 0;
		do {
            loc = World.GetRandomLocation();
			supressed = IsBlocked(loc, resourceSettings.suppressionRange);
			iterations++;
			if(iterations > 10) break;
		} while(supressed);
        return CreateResourceNode(loc,
            Random.Range(resourceSettings.minValue, resourceSettings.maxValue),
            new Color(Random.Range(0, 1.0f), Random.Range(0, 1.0f), Random.Range(0, 1.0f)));
	}

    public ResourceNode CreateResourceNode(Vector3 loc, float value, Color color)
    {
        ResourceNode rn = resourceNodes.Alloc().GetComponent<ResourceNode>();
        rn.SetColor(color);
        rn.transform.position = loc;
        rn.SetValue(value);
        rn.transform.parent = transform;
        return rn;
    }

    void FixedUpdate() {
		while(suppression.Count > 0 && suppression.Peek().expiration < Time.time) {
			suppression.Dequeue();
		}
	}

	void Update () {
		if(timer < resourceSettings.creationDelay) {
			timer += Time.deltaTime;
		}
		if(timer >= resourceSettings.creationDelay) {
			if(activeResources < resourceSettings.maxActive) {
				CreateRandomResourceNode();
				timer -= resourceSettings.creationDelay;
			} else {
				timer = resourceSettings.creationDelay;
			}
		}
	}
}
