using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ResourceEater))]
public class ReleaseEnergy : MonoBehaviour {

    public static int countReleasesPerSprint = 5;
    public static float minimumTransientEnergySeconds = 10;
    public static float secondsIncreasePerRelease = 1;

    ResourceEater eat;
    int numReleases;

	// Use this for initialization
	void Start () {
        eat = GetComponent<ResourceEater>();
	}

    public void Eject(bool forward, float amount)
    {
        for (int i = 0; i < countReleasesPerSprint; ++i) {
            TimeMS.TimerCallback(i*100, () => {
                if(EjectOne(forward, amount)) eat.ChangeMass(-amount);
            });
        }
    }

    bool EjectOne(bool forward, float size)
    {
        if (eat.mass <= size) return false;
        PlayerForce pf = eat.FindComponent<PlayerForce>(true, false);
        Quaternion r = pf.transform.rotation;
        if (!forward)
        {
            r.SetLookRotation(-pf.transform.forward, pf.transform.up);
        }
        World w = World.GetInstance();
        TrailRenderer mommaTrail = eat.FindComponent<TrailRenderer>(false, true);
        ResourceNode n = w.spawner.CreateResourceNode(transform.position + Random.insideUnitSphere * eat.radius, size, eat.color);
        Rigidbody rb = n.gameObject.GetComponent<Rigidbody>();
        if (!rb) { rb = n.gameObject.AddComponent<Rigidbody>(); }
        TrailRenderer tr = n.gameObject.GetComponent<TrailRenderer>();
        tr.material = mommaTrail.material;
        tr.startWidth = eat.radius * 0.0625f;
        tr.endWidth = 0;
        tr.time = 0.125f;
        Seeker s = n.gameObject.AddComponent<Seeker>();
        s.Setup(transform, pf.maxSpeed * 2.5f, pf.maxSpeed * 8);
        rb.velocity = n.transform.forward * pf.maxSpeed;
        n.creator = eat.gameObject;
        int msDelay = (int)(minimumTransientEnergySeconds * 1000 + (numReleases * secondsIncreasePerRelease * 1000));
        print(msDelay);
        TimeMS.TimerCallback(msDelay, () => {
            if (n.creator)
            {
                ReduceOrbitCount(1);
            }
            n.creator = null;
        });
        numReleases++;
        return true;
    }

    public void ReduceOrbitCount(int n) { numReleases -= n; print(numReleases); }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            float amnt = eat.radius * eat.radius;
            if (amnt < 1) amnt = 1;
            Eject(false, amnt);
        }
    }
}
