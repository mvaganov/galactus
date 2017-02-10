using UnityEngine;
using System.Collections;

public class World : MonoBehaviour {

    private static World instance;
    public static World GetInstance() { return instance; }

    public static float SIZE_MODIFIER = 1;
    public static float MASS_MODIFIER = 0.25f;
    public static float SPEED_LIMIT = 150;
    public static float PROJECTILE_SPEED_LIMIT = 350;
    /// <summary>when X an damage particle is sent, it costs X*DAMAGE_ENERGY_COST_RATIO energy to send</summary>
    public static float DAMAGE_ENERGY_COST_RATIO = 0.5f;
    public static float TELEPORT_IDEAL_SIZE = 100;
    /// <summary>at the ideal teleport size, this is teleport distance traveled per unit of energy</summary>
    public static float TELEPORT_MINIMUM_COST_EFFICIENCY = 128.0f;
    public static float TELEPORT_VIABILITY_RANGE = 100;
    public static float MAX_RESOURCE_LIFETIME_IN_SECONDS = 120;

    public ResourceMaker spawner;
    SphereCollider sc;

	// Use this for initialization
	void Start () {
        sc = GetComponent<SphereCollider>();
        if (instance) {
            throw new System.Exception("singleton already set... is there more than one World?");
        }
        instance = this;
	}

	public ParticleSystem attackParticle;

	void OnTriggerExit(Collider other) {
        int i = 1; if(i == 1)return;
        Rigidbody rb = other.GetComponent<Rigidbody>();
        string extra = "active?"+other.gameObject.activeInHierarchy+" ";
        if (rb) extra += "velocity: "+rb.velocity.ToString();
		Debug.LogError(other.gameObject+" left! "+extra);
	}

    public static Vector3 GetRandomLocation() {
        Vector3 loc = Random.onUnitSphere;
        loc *= Random.Range(0, instance.sc.radius);
        return loc;
    }
}
