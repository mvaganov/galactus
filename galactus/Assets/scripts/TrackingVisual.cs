using UnityEngine;
using System.Collections;

public class TrackingVisual : MonoBehaviour {

	GameObject lineX, lineY, lineZ, lineDelta, linePos, lineRLMove;

	public Transform anchorOrigin, eye;

	void Start () {
		//sc = GetComponent<SphereCollider> ();
		LineRenderer lr = Lines.MakeCircle (ref lineX, Vector3.zero, Vector3.right, Color.red, transform.localScale.x, transform.localScale.x * 0.005f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.MakeCircle (ref lineY, Vector3.zero, Vector3.up, Color.green, transform.localScale.y, transform.localScale.y * 0.005f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.MakeCircle (ref lineZ, Vector3.zero, Vector3.forward, Color.blue, transform.localScale.z, transform.localScale.z * 0.005f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.Make (ref lineDelta, Vector3.zero, Vector3.forward, Color.white, 0.001f, 0.001f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;

		lr = Lines.MakeCircle(ref linePos, Vector3.zero, eye.up, Color.gray, 0.05f, 0.01f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.Make (ref lineRLMove, Vector3.zero, Vector3.zero, Color.gray, 0.03f, 0.03f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
	}
	
	// Update is called once per frame
	void Update () {
//		transform.rotation = eye.rotation;
		Vector3 delta = eye.position - anchorOrigin.position;
		delta.x *= transform.localScale.x;
		delta.y *= transform.localScale.y;
		delta.z *= transform.localScale.z;
//		Lines.Make (ref lineDelta, Color.white, delta, delta + eye.forward * 0.02f, 0.01f, 0);
		Lines.MakeCircle(ref linePos, delta, eye.up, Color.gray, 0.05f, 0.01f);
		Lines.Make (ref lineRLMove, Vector3.zero, delta, Color.gray, 0.03f, 0.03f);
		Lines.Make (ref lineDelta, Vector3.zero, eye.forward * 0.02f, Color.white, 0.01f, 0);
	}
}
