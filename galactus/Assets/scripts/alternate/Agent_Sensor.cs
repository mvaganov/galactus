using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Agent_SizeAndEffects))]
public class Agent_Sensor : MonoBehaviour {

	public float range = 500;
	public float radiusExtra = 20;
	public float sensorUpdateTime = 1.0f / 32;
	private float sensorTimer = 0;
	// TODO rename owner->sizeAndEffects
	[HideInInspector]
	public Agent_SizeAndEffects owner;
	public float randomSensorBeamSpread = 0;

//	private GameObject testRay;

	public struct SensorSnapshot
	{
		public RaycastHit[] sensed;
		public float timestamp;
		public SensorSnapshot(float time, RaycastHit[] data){
			this.timestamp=time;
			this.sensed=data;
		}
	}
	private SensorSnapshot recent;

	public SensorSnapshot GetSnapshot() {
		return recent;
	}

	public SensorSnapshot TakeSnapshot(Vector3 origin, Vector3 direction) {
		Ray r = new Ray (origin, direction);
		RaycastHit[] hits = Physics.SphereCastAll (r, owner.GetRadius () + radiusExtra, range + owner.GetRadius (), owner.gameObject.layer);
		return new SensorSnapshot (Time.time, hits);
	}

	public SensorSnapshot TakeSnapshot() {
		Vector3 dir = transform.forward;
		if (randomSensorBeamSpread != 0) {
			dir = Quaternion.AngleAxis (Random.Range (0, randomSensorBeamSpread), Random.onUnitSphere) * dir;

		}
//		Lines.Make (ref testRay, transform.position, transform.position + dir * range, Color.white);
		return TakeSnapshot(transform.position, dir);
	}

	public void EnsureOwnerIsKnown() {
		owner = GetComponent<Agent_SizeAndEffects> ();
	}

	// Use this for initialization
	void Start () {
		EnsureOwnerIsKnown ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		sensorTimer -= Time.deltaTime;
		if (sensorTimer <= 0) {
			sensorTimer = sensorUpdateTime;
			recent = TakeSnapshot();
		}
	}
}
