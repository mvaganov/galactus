using UnityEngine;
using System.Collections;

public class TrackingVisual : MonoBehaviour {

	SphereCollider sc;

	GameObject lineX, lineY, lineZ, lineDelta, linePos, lineRLMove;

	public Transform anchorOrigin, eye;

	// Use this for initialization
	void Start () {
		sc = GetComponent<SphereCollider> ();
		LineRenderer lr = Lines.MakeCircle (ref lineX, Color.red, Vector3.zero, Vector3.right, transform.localScale.x, transform.localScale.x * 0.005f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.MakeCircle (ref lineY, Color.green, Vector3.zero, Vector3.up, transform.localScale.y, transform.localScale.y * 0.005f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.MakeCircle (ref lineZ, Color.blue, Vector3.zero, Vector3.forward, transform.localScale.z, transform.localScale.z * 0.005f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.Make (ref lineDelta, Color.white, Vector3.zero, Vector3.forward, 0.001f, 0.001f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;

		lr = Lines.MakeCircle(ref linePos, Color.gray, Vector3.zero, eye.up, 0.05f, 0.01f);
		lr.useWorldSpace = false;
		lr.transform.parent = transform;
		lr = Lines.Make (ref lineRLMove, Color.gray, Vector3.zero, Vector3.zero, 0.03f, 0.03f);
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
		Lines.MakeCircle(ref linePos, Color.gray, delta, eye.up, 0.05f, 0.01f);
		Lines.Make (ref lineRLMove, Color.gray, Vector3.zero, delta, 0.03f, 0.03f);
		Lines.Make (ref lineDelta, Color.white, Vector3.zero, eye.forward * 0.02f, 0.01f, 0);
	}
}
