using UnityEngine;
using System.Collections.Generic;

public class Group : MonoBehaviour {

	public List<AgentForce> members = new List<AgentForce>();
	public Color color;
	public Sprite icon;
	[SerializeField]
	private bool autoDisbandIfEmpty = true;

	public void AddMember(AgentForce pf) {
		members.Add(pf);
	}
	public void RemoveMember(AgentForce pf) {
		members.Remove(pf);
		if(members.Count == 0 && autoDisbandIfEmpty) {
			Singleton.Get<GroupManager> ().Remove (this);
		}
	}

	public void Startup(string name) {
		this.name = name;
		Sprite[] icons = Singleton.Get<GroupManager>().GetIcons();
		icon = icons[Random.Range(0, icons.Length)];
		color = Random.ColorHSV();
	}

	void Start () {

	}

	void Update () {
		// TODO calculate stats of team and team members intermitently (every few seconds?), like: total size, avg size, avg position, avg velocity, ...
		// TODO keep track of history of those stats, so that fancy charts and graphs can be made!
	}
}
