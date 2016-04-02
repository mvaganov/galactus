using UnityEngine;
using System.Collections;

public class World : MonoBehaviour {

    private static World instance;
    public static World GetInstance() { return instance; }

    public static float SIZE_MODIFIER = 1;
    public static float MASS_MODIFIER = 0.25f;
    public static float SPEED_LIMIT = 150;

    public ResourceMaker spawner;
    SphereCollider sc;

    public static void ResetTrailRenderer(TrailRenderer tr) {
        if (tr) { float oldTime = tr.time; tr.time = 0; TimeMS.TimerCallback(100, () => { tr.time = oldTime; }); }
    }

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
