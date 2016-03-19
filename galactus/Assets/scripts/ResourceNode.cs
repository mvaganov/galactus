using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour {

	public float value = 1;

    public void SetEdible(bool edible) { this.enabled = edible; }

    public GameObject creator = null;

	public float GetValue() { return value; }

	public void SetValue(float v) {
		value = v;
		GetComponent<SphereCollider>().radius = Mathf.Sqrt(v);
		GetComponent<ParticleSystem>().startSize = Mathf.Sqrt(v);
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
		if(re != null && re.gameObject != creator) {
			re.AddValue(value);
            MemoryPoolItem.Destroy(gameObject);
        }
    }
}
