using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingEntity_AI : MonoBehaviour {
	MovingEntity me;

	public Transform target;
	public Vector3 targetPoint;
	public bool useTargetPoint = false;
	public bool useRandomWalkWithoutTarget = false;

	public MovingEntity.DirectionMovementIsPossible moveType;

	float timer = 0;
	public float aiUpdateTimer = .5f;
	// Use this for initialization
	void Start () {
		me = GetComponent<MovingEntity>();
	}
	
	public void SetSeekLocation(Vector3 v) {
		targetPoint = v;
		useTargetPoint = true;
	}

	public void ClearSeekLocation() {
		useTargetPoint = false;
	}

	// Update is called once per frame
	void FixedUpdate () {
		timer += Time.deltaTime;
		if(timer >= aiUpdateTimer) {
			timer -= aiUpdateTimer;
			if(useTargetPoint) {
				me.Seek(targetPoint, moveType);
			} else if(target) {
				me.Seek(target.position, moveType);
			} else if(useRandomWalkWithoutTarget) {
				me.RandomWalk();
			} else {
				me.Seek(transform.position, moveType);
			}
		}
	}
}
