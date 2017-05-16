using UnityEngine;
using System.Collections.Generic;

public class Team : MonoBehaviour {

	public List<TeamMember> members = new List<TeamMember>();
	public Color color;
	public Sprite icon;
	[SerializeField]
	private bool autoDisbandIfEmpty = true;

	public bool AddMember(TeamMember member) {
		// TODO replace with a Set
		if (!members.Contains (member)) {
			members.Add (member);
			return true;
		}
		return false;
	}

	public bool RemoveMember(TeamMember member) {
		if (members.Remove (member)) {
			if (members.Count == 0 && autoDisbandIfEmpty) {
				Singleton.Get<TeamManager> ().Remove (this);
			}
			return true;
		}
		return false;
	}

	public void Startup(string name) {
		this.name = name;
		Sprite[] icons = Singleton.Get<TeamManager>().GetIcons();
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
