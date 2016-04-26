using UnityEngine;
using System.Collections.Generic;

public class Team : MonoBehaviour {

    public List<PlayerForce> members = new List<PlayerForce>();
    static List<Team> allTeams = new List<Team>();
    public Color color;
    public Sprite icon;

    public void AddMember(PlayerForce pf) {
        members.Add(pf);
    }
    public void RemoveMember(PlayerForce pf) {
        members.Remove(pf);
        if(members.Count == 0) {
            allTeams.Remove(this);
        }
    }

    void Startup(string name) {
        this.name = name;
        World w = World.GetInstance();
        // TODO put all the team icons someplace other than world. some other global object, just for data like this. maybe put names in there too. or better yet, complete team descriptions.
        icon = w.teamIcons[Random.Range(0, w.teamIcons.Length)];
        color = Random.ColorHSV();
    }

	// Use this for initialization
	void Start () {
	
	}
	
	void Update () {
        // TODO calculate stats of team and team members intermitently (every few seconds?), like: total size, avg size, avg position, avg velocity, ...
        // TODO keep track of history of those stats, so that fancy charts and graphs can be made!
	}

    public static string RandomName() {
        return PlayerMaker.RandomName();
    }

    public static Team NewTeam() { return NewTeam(RandomName()); }

    public static Team NewTeam(string name) {
        GameObject team = new GameObject();
        Team t = team.AddComponent<Team>();
        t.Startup(name);
        allTeams.Add(t);
        return t;
    }
}
