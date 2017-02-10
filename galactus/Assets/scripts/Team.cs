using UnityEngine;
using System.Collections.Generic;

public class Team : MonoBehaviour {

    public List<PlayerForce> members = new List<PlayerForce>();
    static List<Team> allTeams = new List<Team>();
    public Color color;
    public Sprite icon;

    public UserSoul leader;

    public void SetLeader(UserSoul leader) { this.leader = leader; }
    public UserSoul GetLeader() { return leader; }

    public void AddMember(PlayerForce pf) {
        members.Add(pf);
    }
    public void RemoveMember(PlayerForce pf) {
        members.Remove(pf);
        if(members.Count == 0) {
            allTeams.Remove(this);
        }
    }

    void Startup(string name, UserSoul leader) {
        this.name = name;
        this.leader = leader;
		Sprite[] icons = Singleton.Get<GroupManager> ().GetIcons ();//GroupManager.GetInstance().GetIcons();
        icon = icons[Random.Range(0, icons.Length)];
        color = Random.ColorHSV();
    }

	// Use this for initialization
	void Start () {
	
	}
	
	void Update () {
        // TODO calculate stats of team and team members intermitently (every few seconds?), like: total size, avg size, avg position, avg velocity, ...
        // TODO keep track of history of those stats, so that fancy charts and graphs can be made!
	}

    public static Team NewTeam() { return NewTeam(NameGen.RandomName(), null); }

    public static Team NewTeam(string name, UserSoul leader) {
        GameObject team = new GameObject();
        Team t = team.AddComponent<Team>();
        t.Startup(name, leader);
        allTeams.Add(t);
        return t;
    }
}
