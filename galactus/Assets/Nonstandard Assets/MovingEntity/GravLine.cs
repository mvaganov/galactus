using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class GravLine : GravSource {

	[ContextMenuItem("Center", "CenterObject")]
	public Transform startTransform;
	[ContextMenuItem("Align", "AlignObject")]
	public Transform endTransform;
	/// <summary>will use the Transforms instead if both specified</summary>
	public Vector3 start, end;
	private Vector3 calcS, calcE;

	void CenterObject() {
		if (startTransform != null && endTransform != null) {
			Vector3 p = (startTransform.position + endTransform.position) / 2;
			transform.position = p;
		}
	}
	void AlignObject () {
		if (startTransform != null && endTransform != null) {
			Vector3 delta = endTransform.position - startTransform.position;
			Quaternion r = Quaternion.LookRotation (delta.normalized);
			transform.rotation = r;
		}
	}
	[ContextMenuItem("Create", "CreateCylinder")]
	public GameObject cylinder;
	void StretchCylinder() {
		Vector3 delta = endTransform.position - startTransform.position; 
		CapsuleCollider cap = GetComponent<CapsuleCollider>();
		cap.height = delta.magnitude;
		Vector3 s = cylinder.transform.localScale;
		float height = 2;
		s.y = delta.magnitude/height;
		cylinder.transform.localScale = s;
	}
	public void SetRadius(float radius){
		CapsuleCollider cap = GetComponent<CapsuleCollider>();
		cap.radius = radius;
		Vector3 s = cylinder.transform.localScale;
		s.x = s.z = radius*2;
		cylinder.transform.localScale = s;
	}
#if UNITY_EDITOR
	void CreateCylinder(){
		CapsuleCollider cap = GetComponent<CapsuleCollider>();
		if(!cap) {
			cap = gameObject.AddComponent<CapsuleCollider>();
			cap.height = 2;
			cap.direction = 2;
		}
		if(cylinder == null){
			cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			DestroyImmediate(cylinder.GetComponent<CapsuleCollider>());
			SetRadius(2);
		}
		cylinder.transform.SetParent(transform);
		cylinder.transform.localPosition = Vector3.zero;
		cylinder.transform.localRotation = Quaternion.Euler(-90,0,0);
		Recalculate();
	}
	void OnRenderObject() {
		Vector3 sS = start, sE = end;
		sS.Scale (transform.lossyScale);
		sE.Scale (transform.lossyScale);
		calcS = transform.rotation * sS + transform.position;
		calcE = transform.rotation * sE + transform.position;
		if(startTransform != null && endTransform != null) {
			calcS = startTransform.position; calcE = endTransform.position;
		}
		Debug.DrawLine (calcS, calcE);
	}
#endif
	public void Recalculate(){
		CenterObject();
		AlignObject();
		StretchCylinder();
	}
	public Vector3 GetClosestPointTo(Vector3 point) {
		if (startTransform != null && endTransform != null) {
			calcS = startTransform.position;
			calcE = endTransform.position;
		} else {
			Vector3 sS = start, sE = end;
			sS.Scale (transform.lossyScale);
			sE.Scale (transform.lossyScale);
			calcS = transform.rotation * sS + transform.position;
			calcE = transform.rotation * sE + transform.position;
		}
		Vector3 delta = calcE - calcS;
		float dist = delta.magnitude;
		Vector3 n = delta / dist;
		Plane p = new Plane(n, point);
		Ray r = new Ray(calcS, n);
		float rayDist;
		p.Raycast(r, out rayDist);
		if(rayDist <= 0) { return calcS; }
		else if(rayDist >= dist) { return calcE; }
		return n * rayDist + calcS;
	}

	public override Vector3 CalculateGravityDirectionFrom (Vector3 point) {
		Vector3 delta = GetClosestPointTo(point) - point;
		return delta.normalized;
	}
}
