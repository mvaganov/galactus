using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonControlledCreator : MonoBehaviour {

	public List<GameObject> thingsToCreate;
	public List<GameObject> things = new List<GameObject>();

	public void Create(int whichThing) {
		Vector3 p = transform.position;
		p += Random.onUnitSphere;
		GameObject obj = Instantiate (thingsToCreate[whichThing], p, transform.rotation);
		things.Add (obj);
	}

	public void DestroyAll() {
		for (int i = 0; i < things.Count; ++i) {
			Destroy (things [i]);
		}
		things.Clear ();
	}
	void Update() {
		if (Input.GetKeyDown (KeyCode.M)) { Create (0); }
		if (Input.GetKey (KeyCode.N)) { Create (1); }
		if (Input.GetKeyUp (KeyCode.B)) { Create (2); }
	}
}
