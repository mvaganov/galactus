using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageSender : MonoBehaviour {

	[System.Serializable]
	public class Message {
		public int id;
		public Color color = Color.white;
		public NetNode source, destination;
		public string text;
	}

	[System.Serializable]
	public class MessageEntry
	{
		public string sourceLabel, destinationLabel;
		public Color color;
		private NetNode source, destination;
		private Message message;
		// TODO read a JSON entry
		public int countToSend = 1;
		public void Send(Network net){
			Packet p = net.CreatePacket (source);
			if (message == null) {
				message = new Message ();
			}
			message.source = source;
			message.destination = destination;
			message.color = color;
			p.message = message;
			countToSend--;
		}
		public bool NeedsToDisambiguate() { return source == null || destination == null; }
		public void Disambiguate(Network net) {
			for (int i = 0; i < net.nodes.Count; ++i) {
				GameObject gon = net.nodes [i].gameObject;
				TMPro.TextMeshPro label = gon.GetComponentInChildren<TMPro.TextMeshPro> ();
				if (label != null) {
					if (label.text == sourceLabel) { source = net.nodes [i]; }
					if (label.text == destinationLabel) { destination = net.nodes [i]; }
				}
			}
		}
	}

	public List<MessageEntry> messages = new List<MessageEntry>();

	public Network net;

	void Start() {
		net = GetComponent<Network> ();
	}

	float timer;
	public float sendTimer = 1;

	void FixedUpdate () {
		timer += Time.deltaTime;
		if (timer > sendTimer) {
			if (messages.Count > 0) {
				for (int i = 0; i < messages.Count; ++i) {
					MessageEntry me = messages [i];
					me.Disambiguate (net);
					if (me.countToSend != 0) {
						me.Send (net);
						break;
					}
				}
			}
			timer = 0;
		}
	}
}
