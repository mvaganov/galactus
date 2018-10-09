using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public class NetNode : MonoBehaviour {
	public class NeighborGate : MonoBehaviour {
		public NetNode node;
		public NetEdge edge;

		private static Vector3 startAngle = Quaternion.AngleAxis(10, Vector3.up) * Vector3.forward;
		public static LineRenderer MakeRing(ref GameObject gameObject, NetNode self) {
			LineRenderer lr = NS.Lines.MakeArc(ref gameObject, 340, 24, Vector3.up, startAngle*self.GetLineRadius(), 
				Vector3.zero, self.settings.lineColor, self.settings.linesize, self.settings.linesize);
			lr.startColor = self.settings.lineColor;
			lr.endColor = self.settings.lineColor;
			return lr;
		}

		public void CalculateCircle(NetNode self, int index, int connectionCount) {
			GameObject go = gameObject;
			MakeRing(ref go, self);
			transform.SetParent(self.transform);
			transform.localPosition = Vector3.zero;
		}
		public static NeighborGate Create(NetNode self, NetNode n, float linesize, NetEdge edge){
			GameObject go = null;
			LineRenderer lr = MakeRing(ref go, self);
			lr.name = "gate";
			lr.useWorldSpace = false;
			NeighborGate gate = go.AddComponent<NeighborGate> ();
			if (edge != null) {
				gate.edge = edge;
			} else {
				gate.edge = NetEdge.Create(self, n);
			}
			gate.node = n;
			return gate;
		}
		public void FixedUpdate() {
			if (IsBroken()) {
				Destroy (gameObject);
				return;
			}
			if (edge.direction != Vector3.zero) {
				transform.rotation = Quaternion.LookRotation ((node!=edge.a)?edge.direction:-edge.direction);
			}
		}
		public bool IsBroken() { return node == null || edge == null; }
		public void Break() {
			if (IsBroken ()) return;
			node = null;
			if (edge != null) { edge.Break (); }
			edge = null;
		}
	}
	public NeighborGate GetNeighbor(NetNode n) {
		for (int i = 0; i < neighborGate.Count; ++i) {
			if (neighborGate [i].node == n) {
				return neighborGate [i];
			}
		}
		return null;
	}
	public NetEdge GetEdge(NetNode n) {
		for (int i = 0; i < edges.Count; ++i) {
			if (edges [i].Other (this) == n) {
				return edges [i];
			}
		}
		return null;
	}
	public Network network;
	public List<NeighborGate> neighborGate = new List<NeighborGate>();
	public List<NetEdge> edges = new List<NetEdge>();
	public List<Packet> packets = new List<Packet>();
	Rigidbody rb;
	float timer;

	[System.Serializable]
	public class Settings{
		[Tooltip("will randomly send off a packet if there are this-many-or-more packets")]
		public int minPackets;
		[Tooltip("how close a node must be to trigger a potential neighbor pairing")]
		public float neighborTriggerRadius = 1.5f;
		[Tooltip("how strongly to move toward the center of community")]
		public float communityForce = 0.125f;
		[Tooltip("how strongly to move away from nodes that are too close")]
		public float spreadForce = 1;
		[Tooltip("time to wait between packet shuffleing. is divided by the number of packets in the server")]
		public float switchLag = 1f/4;
		[Tooltip("how much empty space to keep for this node")]
		public float baseRadius = 1f/2;
		[Tooltip("how far from the base radius the packets should orbit at")]
		public float orbitRadius = 1f/8;
		[Tooltip("how far from the base radius the path rings should be")]
		public float lineradius = 1f/32;
		[Tooltip("how far the other node needs to be before the edge breaks")]
		public float edgeBreakDistance = 5;
		[Tooltip("how far the other node needs to be for comfort")]
		public float maxComfortablyFarFromNeighbor = 4;
		[Tooltip("how far the other node needs to be for comfort")]
		public float uncomfortablyCloseToNeighbor = 3;
		[Tooltip("color of the node")]
		public Color color = new Color(137f/255,224f/255,255f/255);
		[Tooltip("color of the path rings")]
		public Color lineColor = Color.black;
		[Tooltip("size of the path rings")]
		public float linesize = 1f/16;
		[Tooltip("maximum number of neighbors")]
		public int maxNeighbors = 2;
		[Tooltip("how close a packet needs to be before a node really considers it 'mine'")]
		public float inventoryRadius = 1;
		public bool useNeighborGates = true;
		public bool showEdges = true;
	}
	public Settings settings = null;

	// TODO remove this
	public MessageSender.Message messageToSend;

	public string GetLabel() {
		TMPro.TextMeshPro label = gameObject.GetComponentInChildren<TMPro.TextMeshPro> ();
		if(label != null){ return label.text; }
		return null;
	}

	public float GetBaseRadius() { return settings.baseRadius * transform.lossyScale.z; }
	public float GetOrbitRadius() { return GetBaseRadius() + settings.orbitRadius; }
	public float GetLineRadius(){ return GetBaseRadius() + settings.lineradius; }

	void Start () {
		if (settings == null) {
			settings = new Settings ();
		}
		rb = GetComponent<Rigidbody>();
		rb.useGravity = false;
		timer += Random.Range(0, settings.switchLag);
		Renderer r = GetComponent<Renderer> ();
		r.material.color = settings.color;
		// check to see if there is a trigger rigidbody. if not, create one!
		SphereCollider triggerSphere = GetComponent<SphereCollider> ();
		if (!triggerSphere) { triggerSphere = gameObject.AddComponent<SphereCollider> (); }
		triggerSphere.isTrigger = true;
		triggerSphere.radius = settings.neighborTriggerRadius;
	}

	void FixedUpdate () {
		Vector3 pos = Vector3.zero;
		int n = 0;
		Vector3 delta;
		float dist;
		for(int i=0; i < edges.Count; ++i) {
			if (edges [i].surfaceDistance > settings.maxComfortablyFarFromNeighbor) {
				pos += edges [i].Other(this).transform.position;
				n++;
			}
		}
		if (n == 0) {
			delta = Vector3.zero;
			dist = 0;
		} else {
			pos /= n;
			delta = pos - transform.position;
			dist = delta.magnitude;
		}
		if (dist > 0.125f) {
			rb.velocity += (delta / dist) * settings.communityForce * Time.deltaTime;
		}

		if (timer >= settings.switchLag) {
			if (messageToSend != null && messageToSend.destination != null && messageToSend.destination != this) {
				//if (packets.Count > 0) {
				Packet p = network.prefabPacket;//packets [0];
				p = (Instantiate (p.gameObject, transform.position, transform.rotation) as GameObject).GetComponent<Packet> ();
				p.transform.SetParent (transform);
				p.current = this;
				p.message = messageToSend;
				p.defaultColor = messageToSend.color;
				p.colorAfterChange = messageToSend.color;
				p.SwitchTo (null);
				timer = 0;
				//}
			} else {
				int packetToSend;
				if (packets.Count > settings.minPackets && edges.Count > 0
				&& (packetToSend = FirstPacketWithin (settings.inventoryRadius)) != -1) {
					Packet p = packets [packetToSend];
					// TODO if the packet has a destination
					// find the fastest path
					// give the packet the next node in the path
					int neighborToGoTo = (int)Random.Range (0, edges.Count);
					NetNode next = edges [neighborToGoTo].Other(this);
					p.SwitchTo (next);
					timer = 0;
				}
			}
		} else {
			timer += Time.deltaTime;// * packets.Count;
		}
	}

	public List<NetNode> PathTo(NetNode n) {
		if (n == null) {
			return new List<NetNode>(){n};
		}
		return network.navdata [this].Path (n);
	}

	public float GetTravelCost() {
		return packets.Count;
	}

	int FirstPacketWithin(float radius) {
		if (packets != null) {
			Vector3 d;
			for (int i = 0; i < packets.Count; ++i) {
				if (packets [i] != null && (packets[i].message == null || packets[i].message.destination != this)) {
					d = packets [i].transform.position - transform.position;
					float dist = d.magnitude;
					if (dist < radius) {
						return i;
					}
				}
			}
		}
		return -1;
	}

	NetEdge AddNeighbor(NetNode otherNode, bool forced, NetEdge edge) {
		if(forced 
		|| edges.Count < settings.maxNeighbors && GetEdge(otherNode) == null) {
			if (settings.useNeighborGates) {
				NeighborGate neighbor = NeighborGate.Create (this, otherNode, settings.linesize, edge);
				neighborGate.Add (neighbor);
				for (int n = 0; n < neighborGate.Count; ++n) {
					neighborGate [n].CalculateCircle (this, n, neighborGate.Count);
				}
				edge = neighbor.edge;
				edges.Add (edge);
			} else {
				NetEdge e = edge;
				if(e == null){
					e = NetEdge.Create (this, otherNode);
				}
				edges.Add (e);
			}
			if (network) { network.UpdateEdgeForm (this, otherNode); }
			return edge;
		}
		return null;
	}

	public bool RemoveNeighbor(NetNode node) {
		for (int n = 0; n < neighborGate.Count; ++n) {
			if (neighborGate [n].node == node) {
				neighborGate [n].Break();
				NeighborGate neighbor = neighborGate [n];
				neighborGate.RemoveAt (n);
				return true;
			}
		}
		for (int e = 0; e < edges.Count; ++e) {
			if (edges [e].Other (this) == node) {
				edges [e].Break ();
				edges.RemoveAt (e);
				return true;
			}
		}
		return false;
	}

	void OnTriggerStay(Collider c) {
		if (rb == null) return;
		NetNode other = c.GetComponent<NetNode>();
		if(other != null && c.gameObject != gameObject) {
			Vector3 delta = transform.position - c.transform.position;
			float distance = delta.magnitude;
			if (distance < settings.uncomfortablyCloseToNeighbor) {
				Vector3 dir = delta / distance;
				rb.velocity += dir * Time.deltaTime * settings.spreadForce;
			}
			bool forcedNeighbor = edges.Count == 0 || other.edges.Count == 0;
			NetEdge e = this.AddNeighbor (other, forcedNeighbor, null);
			if (e != null) {
				other.AddNeighbor (this, true, e);//n.connectionLine);
			}
		}
	}
}
