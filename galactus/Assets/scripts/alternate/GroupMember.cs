using UnityEngine;
using System.Collections;

public class GroupMember : MonoBehaviour {
	public Group team;

	public bool createOwnTeam = true;

	void Start() {
		this.name = NameGen.RandomName ();
		if (createOwnTeam) {
			JoinTeam(Singleton.Get<GroupManager> ().NewGroup (this.name));
		}
	}

	public void JoinTeam(Group team) {
		this.team = team;
		if (team) {
			GetComponent<Agent_SizeAndEffects> ().SetEffectColor (team.color);
		} else {
			Agent_SizeAndEffects a = GetComponent<Agent_SizeAndEffects> ();
			a.SetEffectColor (a.GetColor());
		}
	}
}
