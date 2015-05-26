using UnityEngine;
using System.Collections;

public class ResourceEater : MonoBehaviour {

	public float score = 0;
	public GameObject playerObject;
	public World w;

	float currentAttackPower = 0;

	private static Vector3 one = new Vector3(.1f, .1f, .1f);

	void Start() {
		if(GetComponent<PlayerForce>() != null) {
			playerObject = gameObject;
		} else if(transform.parent != null && transform.parent.GetComponent<PlayerForce>()) {
			playerObject = transform.parent.gameObject;
		}
		GetComponent<SphereCollider>().isTrigger = true;
	}

	public void AddValue(float v) {
		score += v;
		if(score <= 0) {
			print(playerObject.name + " should be dead!");
		} else {
			float mass = score * .1f + 1;
			playerObject.GetComponent<Rigidbody>().mass = mass;
			//sc.radius = 
			// circle area = pi * rad * rad
			// sqrt(circle area / pi) = rad
			float rad = Mathf.Sqrt(mass / Mathf.PI);
			playerObject.transform.localScale = new Vector3(rad, rad, rad);
		}
	}

	void OnTriggerEnter(Collider c) {
		Attack(c.gameObject.GetComponent<ResourceEater>());
	}

	void Attack(ResourceEater e) {
		if(e == null) { return; }
		print(playerObject.name + " attacks " + e.playerObject.name);
		// TODO score * 0.9 could be in a variable called 'minimumEatableSize' or something
		if(e.score >= 0 && e.score < (score * 0.9f)) {
			float distance = Vector3.Distance(e.transform.position, transform.position);
			if(distance < transform.lossyScale.x) {
				AddValue(e.score);
				e.AddValue(-e.score);
				MemoryPoolItem.Destroy(e.playerObject.gameObject);
			}
		}
	}
}
