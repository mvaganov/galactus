using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GroupManager : MonoBehaviour {
	[SerializeField]
	private Sprite[] groupIcons;

	[SerializeField]
	private List<Group> allGroups = new List<Group>();

	public Sprite[] GetIcons() { return groupIcons; }

	public bool Add(Group t) {
		if (!allGroups.Contains (t)) {
			allGroups.Add (t);
			return true;
		}
		return false;
	}

	public bool Remove(Group t) {
		return allGroups.Remove (t);
	}

	public Group NewGroup(string name) {
		GameObject team = new GameObject();
		Group g = team.AddComponent<Group>();
		g.Startup(name);
		Add(g);
		return g;
	}

	// TODO create a random name generator for groups...
	public Group NewGroup() { return NewGroup(NameGen.RandomName()); }

	void Start () { }
	
	void Update () {}
}
