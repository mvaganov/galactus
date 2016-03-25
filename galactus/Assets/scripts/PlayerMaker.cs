using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMaker : MonoBehaviour {

    // TODO if there are no user controlled players, and there is a user controlled camera, create a player for that camera and set it up.

	public MemoryPool<GameObject> players;
	MemoryPool<GameObject> userplayers;

	public PlayerForce[] player_prefab;
	public PlayerForce userControlledPlayer_prefab;

	int activeAgents = 0;

	SphereCollider sc;

    public string[] namePrefix = { "","","","","","","","the","mr.","mrs.","sir.","ms.","lady","my","ur" };
    public string[] nameFragments = { "","butt","poop","troll","lol","noob","dude","swag","super","haxor","red","green","blue","lady","leet" };
    public string[] nameSuffix = { "","","","","","","","ed","ly","dude","man","TheGreat","lady","guy" };
    public string RandomName() {
        string n = "";
        do {
            n = namePrefix[Random.Range(0, namePrefix.Length)];
            n += nameFragments[Random.Range(0, nameFragments.Length)];
            if(Random.Range(0, 2) == 0)
                n += nameFragments[Random.Range(0, nameFragments.Length)];
            n += nameSuffix[Random.Range(0, nameSuffix.Length)];
        } while (n.Length == 0);
        return n;
    }

	[System.Serializable]
	public class Settings {
		public int maxActive = 10;
		public float creationDelay = 1;
	}

	float timer;

	public Settings settings = new Settings();

	void Start() {
		sc = GetComponent<SphereCollider>();
		players = new MemoryPool<GameObject>();
		players.Setup(
			() => {
				int randomIndex = Random.Range(0, player_prefab.Length);
				GameObject original = player_prefab[randomIndex].gameObject;
				GameObject go = Instantiate(original);
				go.name = original.name + " " + activeAgents;
                ResourceEater re = go.transform.GetChild(0).GetComponent<ResourceEater>();
                re.name = RandomName();
				return go;
			},
			(obj) => {
                obj.SetActive(true); activeAgents++;
            },
			(obj) => {
                obj.SetActive(false); activeAgents--;
            },
			(obj) => Object.Destroy(obj)
		);
	}

	public bool IsBlocked(Vector3 testLoc) {
		foreach(GameObject p in players.GetAllObjects()) {
			if(p.activeInHierarchy) {
				float dist = (testLoc - p.transform.position).magnitude;
				if(dist < p.transform.localScale.x) return true;
			}
		}
		return false;
	}

	public PlayerForce CreateRandomPlayer() {
		PlayerForce p = players.Alloc().GetComponent<PlayerForce>();
		Vector3 loc = Vector3.zero;
		bool supressed = false;
		int iterations = 0;
		do {
			loc = Random.onUnitSphere;
			loc *= Random.Range(0, sc.radius);
			supressed = IsBlocked(loc);
			iterations++;
			if(iterations > 10) break;
		} while(supressed);
		p.transform.position = loc;
		return p;
	}

	void Update () {
		if(timer < settings.creationDelay) {
			timer += Time.deltaTime;
		}
		if(timer >= settings.creationDelay) {
			if(activeAgents < settings.maxActive) {
				CreateRandomPlayer();
				timer -= settings.creationDelay;
			} else {
				timer = settings.creationDelay;
			}
		}
	}
}
