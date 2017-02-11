using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameRules : MonoBehaviour {

	public float maxResourceLifetime = 120;
    public float sizeToEnergyRatio = 1 / 32.0f;
    public float massToEnergyRatio = 0.25f;
    public float agentSpeedLimit = 150;
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
}
