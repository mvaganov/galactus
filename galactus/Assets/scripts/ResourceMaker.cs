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

	SphereCollider sc;

	[System.Serializable]
	public class ResourceSettings {
		public int maxActiveResources = 100;
		public float resourceCreationDelay = 1;
		public float suppressionDuration = 30;
		public float suppressionRange = 10;
		public float minValue = 1, maxValue = 10;
	}

	public ResourceSettings resourceSettings = new ResourceSettings();

	float timer = 0;
	int activeResources = 0;

	void Start() {
		sc = GetComponent<SphereCollider>();
		resourceNodes = new MemoryPool<GameObject>();
		resourceNodes.Setup(
			() => Instantiate(resourceNode_prefab.gameObject),
			(obj) => { obj.SetActive(true); activeResources++; },
			(obj) => { 
				obj.SetActive(false);
				activeResources--;
				AddSuppression(obj.transform.position);
				Harvest(obj.GetComponent<ResourceNode>());
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
		resourceNodeHarvest.Emit((int)(v * 10));
	}

	public bool Suppressed(Vector3 testLoc, float suppressionRange) {
		foreach(Suppression s in suppression) {
			float dist = (testLoc - s.position).magnitude;
			if(dist < suppressionRange) return true;
		}
		return false;
	}

	public ResourceNode CreateRandomResourceNode() {
		ResourceNode rn = resourceNodes.Alloc().GetComponent<ResourceNode>();
		Vector3 loc = Vector3.zero;
		bool supressed = false;
		int iterations = 0;
		do {
			loc = Random.onUnitSphere;
			loc *= Random.Range(0, sc.radius);
			supressed = Suppressed(loc, resourceSettings.suppressionRange);
			iterations++;
			if(iterations > 10) break;
		} while(supressed);
		rn.SetColor(new Color(Random.Range(0, 1.0f), Random.Range(0, 1.0f), Random.Range(0, 1.0f)));
		rn.transform.position = loc;
		rn.SetValue(Random.Range(resourceSettings.minValue, resourceSettings.maxValue));
		rn.transform.parent = transform;
		return rn;
	}

	void FixedUpdate() {
		while(suppression.Count > 0 && suppression.Peek().expiration < Time.time) {
			suppression.Dequeue();
		}
	}

	void Update () {
		if(timer < resourceSettings.resourceCreationDelay) {
			timer += Time.deltaTime;
		}
		if(timer >= resourceSettings.resourceCreationDelay) {
			if(activeResources < resourceSettings.maxActiveResources) {
				CreateRandomResourceNode();
				timer -= resourceSettings.resourceCreationDelay;
			} else {
				timer = resourceSettings.resourceCreationDelay;
			}
		}
	}
}
