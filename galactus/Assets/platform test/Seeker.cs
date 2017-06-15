using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seeker : MonoBehaviour {
	public GameObject seek;
	private MOB mob;
	private MovingEntity pc;
	void Start () {
		mob = GetComponent<MOB> ();
		pc = GetComponent<MovingEntity> ();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (seek != null) {
			if (mob && mob.enabled) {
				mob.Seek (seek.transform.position, MovingEntityBase.DirectionMovementIsPossible.forwardOnly);
			}
			if (pc && pc.enabled) {
				pc.Seek (seek.transform.position);
			}
		} else {
			if (mob && mob.enabled) {
				mob.RandomWalk(.125f, Vector3.zero);
			}
			if (pc && pc.enabled) {
				pc.RandomWalk();
			}
		}
	}
}
