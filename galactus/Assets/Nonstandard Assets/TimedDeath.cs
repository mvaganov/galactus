using UnityEngine;
using System.Collections;

public class TimedDeath : MonoBehaviour {
	public float lifetime = 10;
	// Use this for initialization
	void Start () {
		Destroy(gameObject, lifetime);
	}
}
