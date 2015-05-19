using UnityEngine;
using System.Collections;

public class ResourceEater : MonoBehaviour {

	public float score = 0;
	public float energy = 0;
	public float power = 0.5f;
	public bool isAttacking = false;
	public World w;

	float currentAttackPower = 0;

	private static Vector3 one = new Vector3(.1f, .1f, .1f);
	public void AddValue(float v) {
		score += v;
		energy += v;
		power += v;
		if(score <= 0) {
			print(this.name + " should be dead!");
		} else {
			float change = v / score;
			transform.localScale += one * change;
			GetComponent<Rigidbody>().mass += change;
		}
	}

	void OnCollisionEnter(Collision col) {
		if(isAttacking) {
			Collider other = col.collider;
			Attack(other.gameObject.GetComponent<ResourceEater>());
		}
	}

	void OnCollisionStay(Collision col) {
		if(isAttacking) {
			Collider other = col.collider;
			Attack(other.gameObject.GetComponent<ResourceEater>());
		}
	}

	void Attack(ResourceEater e) {
		if(e == null) { return; }
		e.AddValue(-currentAttackPower);
		AddValue(currentAttackPower);
		currentAttackPower = 0;
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		isAttacking = Input.GetButton("Fire1");
	}

	void FixedUpdate() {
		if(isAttacking) {
			float transfer = Time.deltaTime;
			energy -= transfer;
			currentAttackPower += transfer;
			w.attackParticle.transform.position = transform.position;
			w.attackParticle.transform.localScale = transform.localScale;
			w.attackParticle.Emit((int)power);
		}
	}
}
