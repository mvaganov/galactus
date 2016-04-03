using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour {

	public float value = 1;
    float lifetime = -1;
    public ResourceEater creator = null;

    public void SetEdible(bool edible) { this.enabled = edible; }
    public bool IsEdible() { return this.enabled; }
    public void SetLifetime(float lifeInSeconds) { lifetime = lifeInSeconds; }

    void FixedUpdate() {
        if (lifetime > 0) {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0) {
                value = 0;
                MemoryPoolItem.Destroy(gameObject);
            }
        }
    }
    public float GetValue() { return value; }

	public void SetValue(float v) {
		value = v;
		GetComponent<SphereCollider>().radius = Mathf.Sqrt(Mathf.Abs(v));
        //GetComponent<ParticleSystem>().startSize = Mathf.Sqrt(v);
        RefreshSize();
    }

    public void RefreshSize()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        bool moving = rb && rb.velocity != Vector3.zero;
        if (moving) {
            GetComponent<ParticleSystem>().startSize = Mathf.Sqrt(value);
        } else {
            GetComponent<ParticleSystem>().startSize = value;
        }
    }

	public void SetColor(Color c) {
		ParticleSystem ps = GetComponent<ParticleSystem>();
		ps.startColor = c;
        TrailRenderer tr = gameObject.GetComponent<TrailRenderer>();
        if (tr)
        {
            tr.material.SetColor("_TintColor", new Color(c.r, c.g, c.b, 0.25f));
        }
    }

    public Color GetColor() {
		return GetComponent<ParticleSystem>().startColor;
	}

	void OnTriggerStay(Collider c) {
        if (!this.enabled) return;
		ResourceEater re = c.gameObject.GetComponent<ResourceEater>();
		if(re != null && re != creator && re.IsAlive()) {
			re.EatResource(this);
        }
    }
}
