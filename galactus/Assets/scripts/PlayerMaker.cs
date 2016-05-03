using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMaker : MonoBehaviour {
	private MemoryPool<GameObject> agents;
	MemoryPool<GameObject> userplayers;

	public PlayerForce[] player_prefab;
	public PlayerForce userControlledPlayer_prefab;
    [Tooltip("An object containing RespawningPlayer, and some kind of camera")]
    public GameObject playerSoul_prefab;
    public UserSoul currentPlayerSoul;

	int activeAgents = 0;

	SphereCollider sc;

    public static string[] namePrefix = { "","","","","","","","the","mr.","mrs.","sir.","ms.","lady","my","ur" };
    public static string[] nameFragments = { "","butt","poop","troll","lol","noob","dude","swag","super","haxor","red","green","blue","lady","leet" };
    public static string[] nameSuffix = { "","","","","","","","ed","ly","dude","man","TheGreat","lady","guy" };
    public static string RandomName() {
        return RandomName(namePrefix, nameFragments, nameSuffix);
    }
    public static string RandomName(string[] namePrefix, string[] nameFragments, string[] nameSuffix) {
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
    private static int uID = 1;

	void Start() {
		sc = GetComponent<SphereCollider>();
		agents = new MemoryPool<GameObject>();
		agents.Setup(
			() => {
				int randomIndex = Random.Range(0, player_prefab.Length);
				GameObject original = player_prefab[randomIndex].gameObject;
				GameObject go = Instantiate(original);
				go.name = "Entity " + uID;
                uID++;
                return go;
			},
			(obj) => {
                obj.SetActive(true); activeAgents++;
				PlayerForce pf = obj.GetComponent<PlayerForce>();
				pf.Rebirth();
            },
			(obj) => {
                obj.SetActive(false); activeAgents--;
            },
			(obj) => Object.Destroy(obj)
		);
        userplayers = new MemoryPool<GameObject>();
        userplayers.Setup(
            () => {
                GameObject original = userControlledPlayer_prefab.gameObject;
                GameObject go = Instantiate(original);
                go.name = "User Entity " + uID;
                uID++;
                return go;
            },
            (obj) => {
                obj.SetActive(true);
				PlayerForce pf = obj.GetComponent<PlayerForce>();
				pf.Rebirth();
            },
            (obj) => {
                obj.SetActive(false);
            },
            (obj) => Object.Destroy(obj)
        );
        if (!currentPlayerSoul)
        {
            currentPlayerSoul = (Instantiate(playerSoul_prefab) as GameObject).GetComponent<UserSoul>();
        }
    }

	public PlayerForce CreateRandomAgent() {
		PlayerForce p = agents.Alloc().GetComponent<PlayerForce>();
        MoveToPositionInUnblockedSpace(p.gameObject);
		return p;
	}

    public PlayerForce CreateRandomPlayerAgent() {
        PlayerForce p = userplayers.Alloc().GetComponent<PlayerForce>();
        MoveToPositionInUnblockedSpace(p.gameObject);
        return p;
    }

    public GameObject MoveToPositionInUnblockedSpace(GameObject obj) {
        Vector3 loc = Vector3.zero;
        bool supressed = false;
        int iterations = 0;
        do {
            loc = Random.onUnitSphere;
            loc *= Random.Range(0, sc.radius);
            supressed = Physics.CheckSphere(loc, obj.transform.lossyScale.x);
            iterations++;
            if (iterations > 10) break;
        } while (supressed);
        obj.transform.position = loc;
        obj.transform.rotation = Random.rotation;
        return supressed?null:obj;
    }

    void FixedUpdate () {
		if(timer < settings.creationDelay) {
			timer += Time.deltaTime;
		}
		if(timer >= settings.creationDelay) {
			if(activeAgents < settings.maxActive) {
				CreateRandomAgent();
				timer -= settings.creationDelay;
			} else {
				timer = settings.creationDelay;
			}
		}
        if (currentPlayerSoul.IsInNeedOfBody()) {
            PlayerForce pf = CreateRandomPlayerAgent();
            currentPlayerSoul.Posess(pf, true);
        }
	}
}
