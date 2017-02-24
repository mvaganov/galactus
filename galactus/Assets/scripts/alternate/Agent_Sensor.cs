﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Agent_SizeAndEffects))]
public class Agent_Sensor : MonoBehaviour {

	public float range = 200;
	public float radiusExtra = 10;
	public float sensorUpdateTime = 1.0f / 32;
	private float sensorTimer = 0;

	[HideInInspector]
	public Agent_SizeAndEffects sizeAndEffects;
	public float randomSensorBeamSpread = 0;

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

	public Transform GetSensedThatIsnt(Transform t) {
		for (int i = 0; i < recent.sensed.Length; ++i) {
			if (recent.sensed [i].transform != t) {
				return recent.sensed [i].transform;
			}
		}
		return null;
	}

	public SensorSnapshot TakeSnapshot(Vector3 origin, Vector3 direction) {
		Ray r = new Ray (origin, direction);
		RaycastHit[] hits = Physics.SphereCastAll (r, sizeAndEffects.GetRadius () + radiusExtra, range, 1 << sizeAndEffects.gameObject.layer);
		return new SensorSnapshot (Time.time, hits);
	}

//	private GameObject testRay;
	public SensorSnapshot TakeSnapshot() {
		Vector3 dir = transform.forward;
		if (randomSensorBeamSpread != 0) {
			dir = Quaternion.AngleAxis (Random.Range (0, randomSensorBeamSpread), Random.onUnitSphere) * dir;
		}
//		Lines.Make (ref testRay, transform.position, transform.position + dir * range, Color.white).name="testRay";
		return TakeSnapshot(transform.position, dir);
	}

	public void EnsureOwnerIsKnown() {
		sizeAndEffects = GetComponent<Agent_SizeAndEffects> ();
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
