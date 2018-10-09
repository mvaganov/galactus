using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetEdge : MonoBehaviour {
	public NetNode a, b;
	public Vector3 direction;
	public float totalDistance, surfaceDistance;
	public float edgeBreakDistance;
	public void Set(NetNode a, NetNode b){this.a=a;this.b=b;}
	public NetNode Other(NetNode n){ return (n == a) ? b : (n == b) ? a : null; }
	public bool Has(NetNode n){return n == a || n == b; }
	public bool Has(NetNode nodea, NetNode nodeb){ return (nodea==a && nodeb==b) || (nodea==b && nodeb==a); }
	public bool breakMe = false;
	public void FixedUpdate() {
		if (IsBroken() || breakMe) {
			Break ();
			Destroy (gameObject);
			return;
		}
		Vector3 delta = b.transform.position - a.transform.position;
		totalDistance = delta.magnitude;
		surfaceDistance = totalDistance - (a.settings.lineradius + b.settings.lineradius);
		if (surfaceDistance > edgeBreakDistance) {
			Break ();
			return;
		}
		direction = delta / totalDistance;
		Vector3 start = direction * a.GetLineRadius() + a.transform.position;
		Vector3 end = direction * (totalDistance - b.GetLineRadius()) + a.transform.position;
		GameObject connectionLine = gameObject;
		LineRenderer lr = NS.Lines.Make (ref connectionLine, start, end, Color.white, a.settings.linesize, b.settings.linesize);
		lr.startColor = a.settings.lineColor;
		lr.endColor = b.settings.lineColor;
	}
//	static int edgeCount = 0;
	public static NetEdge Create(NetNode a, NetNode b) {
//		Debug.Log ("Creating edge "+(edgeCount++));
		GameObject go = null;
		LineRenderer lr = NS.Lines.Make (ref go, a.transform.position, b.transform.position, a.settings.color, a.settings.linesize, b.settings.linesize);
		lr.numCapVertices = 3;
		if (!a.settings.showEdges) { lr.startColor = Color.clear; }
		if (!b.settings.showEdges) { lr.endColor = Color.clear; }
		if (!a.settings.showEdges && !b.settings.showEdges) { lr.enabled = false; }
		NetEdge e = go.AddComponent<NetEdge> ();
		e.name="edge";
		e.Set (a, b);
		e.edgeBreakDistance = Mathf.Min(a.settings.edgeBreakDistance, b.settings.edgeBreakDistance);
		return e;
	}
	public bool IsBroken() { return a == null || b == null; }
	public void Break() {
		if (IsBroken ()) return;
		if (a != null) {
			a.edges.Remove (this);
			a.RemoveNeighbor (b);
		}
		if (b != null) {
			b.edges.Remove (this);
			b.RemoveNeighbor (a);
		}
		// trigger a re-calculation of the Djikstra table
		if(a != null && a.network != null) { a.network.UpdateEdgeBreak (a, b); }
		if(b != null && b.network != null) { a.network.UpdateEdgeBreak (b, a); }
		a = b = null;
	}
	public override string ToString () { return "("+a.name+"->"+b.name+")"; }
}
