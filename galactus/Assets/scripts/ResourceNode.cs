using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour {

	public float value = 1;

	public float GetValue() { return value; }

	public void SetValue(float v) {
		value = v;
		GetComponent<SphereCollider>().radius = v / 2;
		GetComponent<ParticleSystem>().startSize = v;
	}

	public void SetColor(Color c) {
		ParticleSystem ps = GetComponent<ParticleSystem>();
		ps.startColor = c;
	}

	public Color GetColor() {
		return GetComponent<ParticleSystem>().startColor;
	}

	void OnTriggerEnter(Collider c) {
		ResourceEater re = c.gameObject.GetComponent<ResourceEater>();
		if(re != null) {
			re.AddValue(value);
		}
		MemoryPoolItem.Destroy(gameObject);
	}
}
