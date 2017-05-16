using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent_StatChoice : MonoBehaviour {

	public float statsAllocated = 0;

	public Dictionary_string_float statAllocationStrategy = new Dictionary_string_float(){
		{"eatPower_", 0},
		{"eatSize_", 0},
		{"eatRange_", 0},
		{"eatWarmup_", 0},
		{"eatCooldown_", 1},
		{"speed_", 0},
		{"turn_", 0},
		{"accel_", 0},
		{"radControl_", 0},
		{"defense_", 0},
		{"birthcost_", 0},
		{"energySustain_", 0},
		{"penetration_", 0},
		{"energyShare_", 0}
	};

	private Agent_Properties props;

	private Dictionary<string,string> ruleDescriptions;

	public float CalculateStatsAllocated() {
		float sum = 0;
		foreach (var kvp in ruleDescriptions) {
			float val = props [kvp.Key];
			sum += val;
		}
		statsAllocated = sum;
		return sum;
	}

	public float CountStatsToAllocate() {
		return props ["rad"] - statsAllocated;
	}

	public void AllocateAccordingToStrategy(float points) {
		foreach (var kvp in statAllocationStrategy) {
			props.AddToValue (kvp.Key, points * kvp.Value);
		}
		statsAllocated += points;
		//CalculateStatsAllocated ();
	}

	bool ValidateStatAllocationStrategy() {
		float sum = 0;
		foreach (var kvp in statAllocationStrategy) {
			if (!ruleDescriptions.ContainsKey (kvp.Key)) {
				Debug.LogError ("Unknown stat: \'"+kvp.Key+"\' being used in stat allocation strategy");
				return false;
			}
			sum += kvp.Value;
		}
		if (sum > 1) {
			Debug.LogError ("Stat allocation is over 100%, currently "+sum);
			return false;
		}
		return true;
	}

	void Start () {
		props = GetComponent<Agent_Properties> ();
		ruleDescriptions = GameRules.AGENT_RULES [props.typeName].descriptions;
		ValidateStatAllocationStrategy ();
	}
	
	void FixedUpdate () {
		float points = CountStatsToAllocate ();
		if (points > 0) {
			AllocateAccordingToStrategy (points);
		}
	}
}
