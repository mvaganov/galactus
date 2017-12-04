using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingEntity_AI : MonoBehaviour {
	MovingEntity me;

	public Transform target;

	public MovingEntity.DirectionMovementIsPossible moveType;

	float timer = 0;
	public float aiUpdateTimer = .5f;
	// Use this for initialization
	void Start () {
		me = GetComponent<MovingEntity>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		timer += Time.deltaTime;
		if(timer >= aiUpdateTimer) {
			timer -= aiUpdateTimer;
			if(target){
				me.Seek(target.position, moveType);
			} else {
				me.RandomWalk();
			}
		}
	}
}
