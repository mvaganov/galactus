﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(VariableCalculator))]
// TODO rename this to something? something basic? something that makes the agent a rule-follower
public class Agent_StatGameplay : MonoBehaviour {
	[HideInInspector]
	public ValueCalculator<Agent_StatGameplay> adj;
	public Dictionary_string_float properties = new Dictionary_string_float();
	[HideInInspector]
	public Agent_Properties props;
	protected Agent_MOB mob;
	protected Agent_SizeAndEffects sizeAndEffects;
	protected EatSphere eatS;

	[HideInInspector]
	public float baseSpeed, baseTurn, baseAccel, baseEatRadRatio;

	private void RememberBase() {
		baseSpeed = mob.maxSpeed; baseTurn = mob.turnSpeed; baseAccel = mob.acceleration;
		baseEatRadRatio = eatS.GetRadius () / sizeAndEffects.GetRadius ();
	}

	public static ValueCalculator<Agent_Properties>.ChangeListener adjustAndReleaseWithEnergy = delegate(Agent_Properties res, string resourceName, float oldValue, float newValue) {
//		print ("energy changed!   "+oldValue+" -> "+newValue);
		if(newValue <= 0) {
			MemoryPoolItem.Destroy(res.gameObject);
		} else {
			Agent_StatGameplay stat = res.GetComponent<Agent_StatGameplay>();
			stat.NotifyEnergyChanged();
		}
	};

	public void NotifyEnergyChanged() {
		adj.InvalidateAllCalculatedValues ();
		float rad = adj ["rad"];
		if (sizeAndEffects.GetRadius () != rad) {
			sizeAndEffects.SetRadius (rad);
			UpdateEndPointValues ();
		}
	}

	public void UpdateEndPointValues() {
		mob.acceleration = adj ["accel"];
		mob.maxSpeed = adj ["speed"];
		mob.turnSpeed = adj ["turn"];
		EatSphere eat = sizeAndEffects.GetEatSphere ();
		eat.SetRadius(adj["eatSize"]);
		eat.transform.localPosition = new Vector3(0,0,adj ["eatRange"]);
		eat.power = adj ["eatPower"];
		eat.warmup = adj ["eatWarmup"];
		eat.cooldown = adj ["eatCooldown"];
	}

	// TODO calculate which **props** modify which adjustments, and add listeners to the props to invalidate adjustments
	// TODO put all constants into GameRules, with names

	void SetupVariableCalculationLogic() {
		adj = new ValueCalculator<Agent_StatGameplay> (this, properties);
		adj.SetValueRules(GameRules.AGENT_RULES["agent"]);
		// TODO read dependencies from a centralized location
		adj.CalculateValueDependencies ();
	}

	void Start () {
		print ("stats for "+name);
		mob = GetComponent<Agent_MOB> ();
		sizeAndEffects = GetComponent<Agent_SizeAndEffects> ();
		eatS = sizeAndEffects.GetEatSphere ();
		props = GetComponent<Agent_Properties> ();
		RememberBase ();
		SetupVariableCalculationLogic ();

		props.AddValueChangeListener ("energy", adjustAndReleaseWithEnergy);
		props.SetValue ("energy", props ["energy"]);
	}
}
