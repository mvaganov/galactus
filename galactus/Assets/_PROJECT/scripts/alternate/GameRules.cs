using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameRules : MonoBehaviour {

	public static float MINIMUM_PREY_SIZE = 0.75f;

	public float maxResourceLifetime = 120;
    public float sizeToEnergyRatio = 1 / 32.0f;
    public float massToEnergyRatio = 0.25f;
	public float warmupToSizeRatio = 0.25f;
	public float cooldownToSizeRatio = 0.75f;
    public float agentSpeedLimit = 150;
	public float costToCreateAgent = 100;
	public float timeCostToCreateAgent = 100;
    public float projectileSpeedLimit = 350;
    /// <summary>when a damage particle is sent, it costs X*DAMAGE_ENERGY_COST_RATIO energy to send</summary>
    public float damageToEnergyRatio = 0.5f;
	public float energyDrainPercentagePerSecond_MOB = 1 / 64.0f;
	public float energyDrainPercentagePerSecond_Collectable = 1 / 128.0f;
    public float TELEPORT_IDEAL_SIZE = 100;
    /// <summary>at the ideal teleport size, this is teleport distance traveled per unit of energy</summary>
    public float TELEPORT_MINIMUM_COST_EFFICIENCY = 128.0f;
    public float TELEPORT_VIABILITY_RANGE = 100;
	public Dictionary<string,Agent_Properties> savedProperties = new Dictionary<string,Agent_Properties> ();

    SphereCollider sc;

//	public float EnergyDrainPercentageFor(EnergyAgent go) {
//		if (go.GetComponent<Agent_MOB> ()) return energyDrainPercentagePerSecond_MOB;
//		return energyDrainPercentagePerSecond_Collectable;
//	}

	// TODO rename ResourceHolder to Properties
	public void RegisterResourceHolderPrefab(GameObject go) {
		Agent_Properties ap = go.GetComponent<Agent_Properties> ();
		if (!ap)
			throw new UnityException ("can't store null Agent_Prefab in savedProperties dictionary");
		RegisterResourceHolderPrefab (ap.typeName, ap);
	}
	public void RegisterResourceHolderPrefab(string name, Agent_Properties ap) {
		savedProperties [name] = ap;
	}

	public Dictionary_string_float GetDefaultEnergyFor(string typeName) {
		return savedProperties [typeName].GetProperties();
	}

	// Use this for initialization
	void Start () {
        sc = GetComponent<SphereCollider>();
	}

	void OnTriggerExit(Collider other) {
        int i = 1; if(i == 1)return;
        Rigidbody rb = other.GetComponent<Rigidbody>();
        string extra = "active?"+other.gameObject.activeInHierarchy+" ";
        if (rb) extra += "velocity: "+rb.velocity.ToString();
		Debug.LogError(other.gameObject+" left! "+extra);
	}

    public Vector3 GetRandomLocation() {
        Vector3 loc = Random.onUnitSphere;
        loc *= Random.Range(0, sc.radius);
        return loc;
    }

	public class ValueRules {
		public Dictionary<string,string> descriptions;
		/// <summary>define how variables are calculated</summary>
		public Dictionary<string,ValueCalculator<Agent_Properties>.ValueCalculation<float>> calculation;
		/// <summary>define code that runs in response to values being set</summary>
		public Dictionary<string,ValueCalculator<Agent_Properties>.ChangeListener> changeListeners;
		/// <summary>is filled with calculated structure identifying which values depend on which other values, and thus when values should be recalculated</summary>
		public TightBucketsOfUniques<string, string> dependencies;

		public ValueRules(Dictionary<string,ValueCalculator<Agent_Properties>.ValueCalculation<float>> calculation,
			Dictionary<string,ValueCalculator<Agent_Properties>.ChangeListener> changeListeners,
			Dictionary<string,string> descriptions) {
			this.calculation = calculation;
			this.changeListeners = changeListeners;
			this.descriptions = descriptions;
		}
	}

	private static ValueRules agentRules = new ValueRules(
		new Dictionary<string,ValueCalculator<Agent_Properties>.ValueCalculation<float>>() {
			{"baseSpeed",	(a)=>{	return a.HasValue("baseSpeed")?a.GetCached("baseSpeed"):a.mob.MoveSpeed;}},
			{"baseAccel",	(a)=>{	return a.HasValue("baseAccel")?a.GetCached("baseAccel"):a.mob.acceleration;}},
			{"baseTurn",	(a)=>{	return a.HasValue("baseTurn")?a.GetCached("baseTurn"):a.mob.TurnSpeed;}},
			{"baseEatRad",	(a)=>{	return a.HasValue("baseEatRad")?a.GetCached("baseEatRad"):(a.eatS.GetLocalRadius());}},
			{"baseRadius",	(a)=>{	return Mathf.Sqrt (a["energy"]*Singleton.Get<GameRules>().sizeToEnergyRatio);}},
			{"rad",			(a)=>{	float r=a["radControl_"];	return Mathf.Max(r, a["baseRadius"]-r);}},
			{"speed",		(a)=>{	float s=a["speed_"],r=a["rad"],b=a["baseSpeed"];	return (s<=r)?(b / Mathf.Max(1,r-s)):(b+(s-r));}},
			{"turn",		(a)=>{	float t=a["turn_"],r=a["rad"],b=a["baseTurn"];	return (t<=r)?(b / (r-t)):(b+(t-r));}},
			{"accel",		(a)=>{	return a["baseAccel"] + a["rad"] + a["accel_"]; }},
			{"eatSize",		(a)=>{	return a["baseEatRad"]*(a["eatSize_"]+1);}},
			{"eatRange",	(a)=>{	return Mathf.Max(0,0.75f+(a["eatRange_"]-a["eatSize"])/2);}},
			{"eatPower",	(a)=>{	return a["rad"]+a["eatPower_"];}},
			{"eatWarmup",	(a)=>{	return Mathf.Max(Time.fixedDeltaTime, a["rad"]-a["eatWarmup_"]) * Singleton.Get<GameRules>().warmupToSizeRatio;}},
			{"eatCooldown",	(a)=>{	return Mathf.Max(Time.fixedDeltaTime, a["rad"]-a["eatCooldown_"]) * Singleton.Get<GameRules>().cooldownToSizeRatio;}},
			{"defense",		(a)=>{	return a["rad"] + (a["defense_"]);}},
			{"penetration",	(a)=>{	return a["rad"] + (a["penetration_"]*1.5f);}},
			{"share",		(a)=>{	return Mathf.Min(a["energy"]-10, Mathf.Sqrt(a["rad"]) + a["energyShare_"]);}},// TODO implement sharing
			{"birthCost",	(a)=>{	return (Singleton.Get<GameRules>().costToCreateAgent/(a["birthcost_"]+1)) - a["rad"];}}, // TODO implement birth
			{"energyDrain",	(a)=>{	return Mathf.Max(0,a["rad"]-a["energySustain_"])*Singleton.Get<GameRules>().energyDrainPercentagePerSecond_MOB; }}
		},
		new Dictionary<string,ValueCalculator<Agent_Properties>.ChangeListener>(){
			{"rad",(a,name,old,val)=>{a.sizeNeffects.SetRadius(val);}},
			{"accel",(a,name,old,val)=>{a.mob.acceleration = val;}},
			{"turn",(a,name,old,val)=>{a.mob.TurnSpeed = val;}},
			{"speed",(a,name,old,val)=>{a.mob.MoveSpeed = val;}},
			{"eatSize",(a,name,old,val)=>{a.eatS.SetRadius(val);}},
			{"eatRange",(a,name,old,val)=>{a.eatS.transform.localPosition=new Vector3(0,0,val);}},
			{"eatWarmup",(a,name,old,val)=>{a.eatS.warmup = val;}}, // TODO use props["eatWarmup"]
			{"eatCooldown",(a,name,old,val)=>{a.eatS.cooldown = val;}}, // TODO use props["eatCooldown"]
			{"energy",(a,name,old,val)=>{ if(val <= 0) { MemoryPoolItem.Destroy(a.gameObject); } }}
		},
		new Dictionary<string,string>(){
			{"speed_", "Movement Speed\nIncrease maximum-speed"},
			{"turn_", "Turn Speed\nTurn faster"},
			{"accel_", "Acceleration Power\nAccelerate with more force"},
			{"radControl_", "Radius Control\nIf small: set the size of your radius; if large: decrease it"},
			{"eatSize_", "Eat Sphere Radius\nBigger eat-sphere"},
			{"eatRange_", "Eat Sphere Range\nMove your eat-sphere further away"},
			{"eatPower_", "Eat Power\nEat more at once"},
			{"eatWarmup_", "Eat Readiness\nGet-ready-to-eat faster"},
			{"eatCooldown_", "Eat Speed\nRecover-from-eating faster"},
			{"defense_", "Defense\nDefend against attacks better"},
			{"penetration_","Penetration\nBreak through the others\' defenses"},
			{"birthcost_", "Birth Cost Reduction\nReduce energy requirement of creating team members"},
			{"energySustain_", "Energy Sustain\nReduce energy decay rate"},
			{"energyShare_","Energy Share\nIncrease amount of energy shared at once"}
		}
	);
	private static ValueRules resourceRules = new ValueRules(
		new Dictionary<string,ValueCalculator<Agent_Properties>.ValueCalculation<float>>() {
			{"rad",			(a)=>{	return a["energy"];}},
		}, 
		new Dictionary<string,ValueCalculator<Agent_Properties>.ChangeListener>(){
			{"rad",(a,name,old,val)=>{
					float v = Mathf.Sqrt(val);
					a.sizeNeffects.SetRadius(v);
					a.sizeNeffects.SetParticleSpeed(val);
				}},
			{"energy",(a,name,old,val)=>{ if(val <= 0) { MemoryPoolItem.Destroy(a.gameObject); } }}
		},null
	);

	// TODO replace Agent_StateGameplay with Agent_Properties, and remove the need for AgentSateGameplay
	public static Dictionary<string, ValueRules> AGENT_RULES = 
		new Dictionary<string, ValueRules>{
		{"hunter", agentRules},
		{"agent", agentRules},
		{"resource", resourceRules}
	};
}
