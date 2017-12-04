using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AgentMaker : MonoBehaviour {
	private MemoryPool<GameObject> agents;
	private MemoryPool<GameObject> userplayers;

	[Tooltip("what sort of agent is made for the player to control")]
	public Agent_MOB pfab_userAgent;
	[Tooltip("The thing that the player controls agents through")]
	public Agent_InputControl pfab_userController;
	private Agent_InputControl activeController;
	[Tooltip("what the AI get to control")]
	public Agent_MOB[] pfab_agents;

	public float minStartSize = 1, maxStartSize = 1;
	int activeAgents = 0;

	SphereCollider sc;

	[System.Serializable]
	public class SpawnSettings {
		public int maxActive = 10;
		public float creationDelay = 1;
	}

	float timer;

	public SpawnSettings agentSpawnSettings = new SpawnSettings();
    private static int uID = 1;

	void Start() {
		GameRules rules = Singleton.Get<GameRules> ();
		rules.RegisterResourceHolderPrefab (pfab_userAgent.gameObject);
		System.Array.ForEach (pfab_agents, delegate(Agent_MOB obj) {
			rules.RegisterResourceHolderPrefab (obj.gameObject);
		});
		sc = GetComponent<SphereCollider>();
		agents = new MemoryPool<GameObject>();
		agents.Setup(
			() => {
				int randomIndex = Random.Range(0, pfab_agents.Length);
				GameObject original = pfab_agents[randomIndex].gameObject;
				GameObject go = Instantiate(original);
				go.name = "Entity " + uID;
                uID++;
                return go;
			},
			(obj) => {
                obj.SetActive(true); activeAgents++;
				Agent_Properties p = obj.GetComponent<Agent_Properties>();
				p.Reset();
				p.Energy = Random.Range(minStartSize, maxStartSize);
            },
			(obj) => {
                obj.SetActive(false); activeAgents--;
            },
			(obj) => Object.Destroy(obj)
		);
        userplayers = new MemoryPool<GameObject>();
        userplayers.Setup(
            () => {
				GameObject go = Instantiate(pfab_userAgent.gameObject);
                go.name = "User Entity " + uID;
                uID++;
                return go;
            },
            (obj) => {
				obj.SetActive(true); activeAgents++;
				Agent_Properties p = obj.GetComponent<Agent_Properties>();
				p.Reset();
            },
            (obj) => {
                obj.SetActive(false);
            },
            (obj) => Object.Destroy(obj)
        );
		if (!activeController)
        {
			activeController = (Instantiate(pfab_userController.gameObject) as GameObject).GetComponent<Agent_InputControl>();
        }
    }

	public Agent_MOB CreateRandomAgent() {
		Agent_MOB p = agents.Alloc().GetComponent<Agent_MOB>();
        MoveToPositionInUnblockedSpace(p.gameObject);
		return p;
	}

	public Agent_MOB CreateRandomPlayerAgent() {
		Agent_MOB p = userplayers.Alloc().GetComponent<Agent_MOB>();
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
			supressed = Physics.CheckSphere(loc, obj.transform.localScale.x);
            iterations++;
            if (iterations > 10) break;
        } while (supressed);
        obj.transform.position = loc;
        obj.transform.rotation = Random.rotation;
        return supressed?null:obj;
    }

    void FixedUpdate () {
		if(timer < agentSpawnSettings.creationDelay) {
			timer += Time.deltaTime;
		}
		if(timer >= agentSpawnSettings.creationDelay) {
			if(activeAgents < agentSpawnSettings.maxActive) {
				CreateRandomAgent();
				timer -= agentSpawnSettings.creationDelay;
			} else {
				timer = agentSpawnSettings.creationDelay;
			}
		}
		if (activeController.GetControlled() == null) {
			Agent_MOB mob = CreateRandomPlayerAgent();
			activeController.Control (mob);
        }
	}
}
