using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameRules : MonoBehaviour {

	public float maxResourceLifetime = 120;
    public float sizeToEnergyRatio = 1 / 32.0f;
    public float massToEnergyRatio = 0.25f;
	public float warmupToSizeRatio = 0.25f;
	public float cooldownToSizeRatio = 0.75f;
    public float agentSpeedLimit = 150;
	public float costToCreateAgent = 100;
    public float projectileSpeedLimit = 350;
    /// <summary>when a damage particle is sent, it costs X*DAMAGE_ENERGY_COST_RATIO energy to send</summary>
    public float damageToEnergyRatio = 0.5f;
	public float energyDrainPercentagePerSecond_MOB = 1 / 32.0f;
	public float energyDrainPercentagePerSecond_Collectable = 1 / 128.0f;
    public float TELEPORT_IDEAL_SIZE = 100;
    /// <summary>at the ideal teleport size, this is teleport distance traveled per unit of energy</summary>
    public float TELEPORT_MINIMUM_COST_EFFICIENCY = 128.0f;
    public float TELEPORT_VIABILITY_RANGE = 100;
	public Dictionary<string,Agent_Properties> savedProperties = new Dictionary<string,Agent_Properties> ();

    SphereCollider sc;

	public float EnergyDrainPercentageFor(EnergyAgent go) {
		if (go.GetComponent<Agent_MOB> ()) return energyDrainPercentagePerSecond_MOB;
		return energyDrainPercentagePerSecond_Collectable;
	}

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

	// TODO replace Agent_StateGameplay with Agent_Properties, and remove the need for AgentSateGameplay
	public static Dictionary<string, Dictionary<string,ValueCalculator<Agent_StatGameplay>.ValueRules<float>>> AGENT_RULES = 
		new Dictionary<string, Dictionary<string,ValueCalculator<Agent_StatGameplay>.ValueRules<float>>>{
		{"agent", new Dictionary<string,ValueCalculator<Agent_StatGameplay>.ValueRules<float>>() {
			{"baseRadius",(a)=>	{	return Mathf.Sqrt (a.props["energy"]*Singleton.Get<GameRules>().sizeToEnergyRatio);}},
			{"rad", (a) =>		{	float r=a.props["radCtrl"];	return Mathf.Max(r, a.adj["baseRadius"]-r);}},
			{"speed", (a) =>	{	float s=a.props["+speed"];	return a.baseSpeed / Mathf.Max(1,a.adj["rad"]-s);}},
			{"turn", (a) =>		{	float t=a.props["+turn"];	return a.baseTurn / Mathf.Max(1,a.adj["rad"]-t);}},
			{"accel", (a) =>	{	float c=a.props["+accel"];	return a.baseAccel + a.adj["rad"] + c; }},
			{"eatSize", (a) =>	{	return a.adj["rad"]*a.baseEatRadRatio+a.props["+eatRadius"];}},
			{"eatRange", (a) =>	{	return a.adj["rad"]/2+(a.props["+eatRange"])*a.props["eatSize"];}},
			{"eatPower", (a) =>	{	return a.adj["rad"]+a.props["+eatPower"];}},
			{"eatWarmup", (a)=>	{	return Mathf.Max(0, a.adj["rad"]-a.props["+eatWarmup"]) * Singleton.Get<GameRules>().warmupToSizeRatio;}},
			{"eatCooldown",(a)=>{	return Mathf.Max(0, a.adj["rad"]-a.props["+eatCooldown"]) * Singleton.Get<GameRules>().cooldownToSizeRatio;}},
			{"defense", (a)=>	{	return a.adj["rad"]/2 + a.props["+defense"];}},// TODO implement defense, TODO gamerules.defense cost
			{"share", (a)=>		{	return Mathf.Sqrt(a.adj["rad"]);}},// TODO implement sharing
			{"birthCost", (a)=>	{	return Singleton.Get<GameRules>().costToCreateAgent/(a.props["+birthCost"]+1) - a.adj["rad"];}}, // TODO implement birth
		}}
	};

	public static Dictionary<string, TightBucketsOfUniques<string, string>> VALUE_DEPENDENCIES = 
		new Dictionary<string, TightBucketsOfUniques<string, string>>();
}
