using UnityEngine;
using System.Collections;

public class ResourceCollectable : MonoBehaviour {

    public float lifetime = -1;
	public float size = 5;

	public float GetValue() {
		return size;
	}

    void FixedUpdate() {
        if (lifetime > 0) {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0) {
				Agent_Properties h = GetComponent<Agent_Properties> ();
				h.Clear ();
                MemoryPoolItem.Destroy(gameObject);
            }
        }
    }

	public void SetSize(float v) {
		size = Mathf.Abs(size);
		GetComponent<SphereCollider>().radius = Mathf.Sqrt(size);
        RefreshSize();
    }

	public static Agent_Properties.ResourceChangeListener releaseOnDrain = delegate(Agent_Properties res, string resourceName, float oldValue, float newValue) {
		bool hasResourcesStill = false;
		System.Collections.Generic.ICollection<float> values = res.GetValues().Values;
		foreach (float v in values) {
			if (v > 0) { hasResourcesStill = true; break; }
		}
		if (!hasResourcesStill) {
			MemoryPoolItem m = res.GetComponent<MemoryPoolItem> ();
			if (m) { m.FreeSelf (); }
		}
	};

	void Start() {
		SetSize (size);
		Agent_Properties h = GetComponent<Agent_Properties> ();
		h.AddValueChangeListener ("energy", releaseOnDrain);
	}

    public void RefreshSize() {
        Rigidbody rb = GetComponent<Rigidbody>();
        bool moving = rb && rb.velocity != Vector3.zero;
        if (moving) {
			GetComponent<ParticleSystem>().startSize = Mathf.Sqrt(size);
        } else {
			GetComponent<ParticleSystem>().startSize = size;
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
}
