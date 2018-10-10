using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Packet : MonoBehaviour {
	[Tooltip("what the packet is orbiting")]
	public NetNode current;
	[Tooltip("how fast this packet moves")]
	public float speed = 5;
	[Tooltip("a modifier for the center's standard orbit radius")]
	public float orbitRadius = 0;
	[Tooltip("a 'wiggle' factor. making it very high will make the packet look more like lightning")]
	public float orbitSwerve = 0.5f;
	[Tooltip("how closely the packet will try to stay in it's orbit altitude")]
	public float orbitStrength = 3;
	public Color defaultColor = Color.white;
	public Color colorAfterChange = new Color(1,0,0);
	float colorTimer = 0;
	Rigidbody rb;
	TrailRenderer tr;
	public MessageSender.Message message = new MessageSender.Message();


	void Start () {
		rb = GetComponent<Rigidbody>();
		tr = GetComponent<TrailRenderer> ();
        if (current != null) {
            current.packets.Add(this);
        }
		SetColor (defaultColor);
	}

	public void SetColor(Color c){
		tr.startColor = tr.endColor = c;
	}
	
	void FixedUpdate () {
		if (message != null && message.destination == current) {
			Vector3 delta = current.transform.position - transform.position;
			if (delta.magnitude < current.GetOrbitRadius()) {
				current.packets.Remove (this);
				rb.velocity = Vector3.zero;
				speed = 0;
				Destroy (gameObject, tr.time+1);
				return;
			} else {
				orbitRadius = -current.GetOrbitRadius ();
			}
		}
		if (colorTimer > 0) {
			SetColor(Color.Lerp(defaultColor, colorAfterChange, colorTimer));
			colorTimer -= Time.deltaTime;
		}
		Vector3 v = rb.velocity;
		float s = v.magnitude;
		if(s == 0) { v = Random.onUnitSphere; }

		if (current) {
			if (orbitSwerve != 0) {
				v += Random.onUnitSphere * orbitSwerve;
			}
			Vector3 delta = transform.position - current.transform.position;
			float d = delta.magnitude;
			Vector3 normal = delta / d;
			Vector3 idealPosition = current.transform.position + normal * (orbitRadius + current.GetOrbitRadius ());
			Vector3 towardIdeal = idealPosition - transform.position;
			Vector3 towardCenter = normal * Vector3.Dot (v, normal);
			v -= towardCenter;
			v += towardIdeal * orbitStrength;
		} else {
			Debug.LogError ("packet loss");
		}
		rb.velocity = v.normalized * speed;
	}

	public void SwitchTo(NetNode node) {
		colorTimer = 1;
		transform.SetParent(null);
		current.packets.Remove(this);

		if (message != null && message.destination != null) {
			List<NetNode> path = current.PathTo (message.destination);
//			string txt = "";
//			for (int i = 0; i < path.Count; ++i) {
//				txt += path [i].name;
//			}
//			Debug.Log (current.name+" to "+message.destination.name+" : "+txt);
			int index = 0;
			if (path [index] == current)
				index++;
			if (index < path.Count) {
				node = path [index];
			}
		}

		current = node;
		node.packets.Add (this);
		transform.SetParent (node.transform);
	}
}
