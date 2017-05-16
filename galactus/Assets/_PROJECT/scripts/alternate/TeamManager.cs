using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TeamManager : MonoBehaviour {
	[SerializeField]
	private Sprite[] groupIcons;

	[SerializeField]
	private List<Team> allGroups = new List<Team>();

	public Sprite[] GetIcons() { return groupIcons; }

	public bool Add(Team t) {
		if (!allGroups.Contains (t)) {
			allGroups.Add (t);
			t.transform.parent = transform;
			return true;
		}
		return false;
	}

	public bool Remove(Team t) {
		return allGroups.Remove (t);
	}

	public Team NewGroup(string name) {
		GameObject team = new GameObject();
		Team g = team.AddComponent<Team>();
		g.Startup(name);
		Add(g);
		return g;
	}

	// TODO create a random name generator for groups...
	public Team NewGroup() { return NewGroup(NameGen.RandomName()); }

	void Start () { }
	
	void Update () {}
}
