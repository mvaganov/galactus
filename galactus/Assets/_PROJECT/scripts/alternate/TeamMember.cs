using UnityEngine;
using System.Collections;

public class TeamMember : MonoBehaviour {
	public Team team;

	public bool createOwnTeam = true;

	void Start() {
		this.name = NameGen.RandomName ();
		if (createOwnTeam) {
			JoinTeam(Singleton.Get<TeamManager> ().NewGroup (this.name));
		}
	}

	public void JoinTeam(Team team) {
		this.team = team;
		if (team) {
			GetComponent<Agent_SizeAndEffects> ().SetEffectColor (team.color);
		} else {
			Agent_SizeAndEffects a = GetComponent<Agent_SizeAndEffects> ();
			a.SetEffectColor (a.GetColor());
		}
	}
}
