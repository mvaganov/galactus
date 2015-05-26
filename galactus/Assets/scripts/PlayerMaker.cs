using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMaker : MonoBehaviour {

	MemoryPool<GameObject> players;
	MemoryPool<GameObject> userplayers;

	public PlayerForce[] player_prefab;
	public PlayerForce userControlledPlayer_prefab;

	int activePlayers = 0;

	SphereCollider sc;

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
				return Instantiate(player_prefab[randomIndex].gameObject);
			},
			(obj) => { obj.SetActive(true); activePlayers++; },
			(obj) => { obj.SetActive(false); activePlayers--; },
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
			if(activePlayers < settings.maxActive) {
				CreateRandomPlayer();
				timer -= settings.creationDelay;
			} else {
				timer = settings.creationDelay;
			}
		}
	}
}
