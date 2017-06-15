using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent_SizeAndEffects : MonoBehaviour {

	[SerializeField]
	private EatSphere eatSphere;
	private Vector3 eatSphereOffset;
	public float eatSphereRatio = 0;
	private Renderer graphic;
	public EatSphere GetEatSphere() { return eatSphere; }
	private Agent_MOB mob;
	private SphereCollider sphere;

	public delegate void RadiusChangeListener(float oldSize, float newSize);
	public event RadiusChangeListener onRadiusChange;

	void Start() {
		GetMOB();
		GetGraphic();
	}

	public Agent_MOB GetMOB() {
		if(mob == null) { mob = GetComponent<Agent_MOB>(); }
		return mob;
	}

	public SphereCollider GetOwnSphere() {
		if(sphere == null) { sphere = GetComponent<SphereCollider>(); }
		return sphere;
	}

	public Renderer GetGraphic() {
		if(graphic == null) {
			graphic = GetComponent<Renderer>();
			if(!graphic.material.HasProperty("_Color")) {
				// find a child graphic object. pick the one with the fewest scripts that inclues a (MeshFilter and MeshRenderer) or (TODO) Sprite
				Transform t;
				int bestCount = -1;
				for(int i = 0; i<transform.childCount; ++i) {
					t = transform.GetChild(i);
					Renderer r = t.GetComponent<Renderer>();
					if(!r.material.HasProperty("_Color")) {
						continue;
					}
					if(t.name == "graphic") {
						graphic = r;
						break;
					}
					int componentCount = t.GetComponents<Component>().Length;
					if(r != null && (bestCount< 0 || componentCount < bestCount)) {
						graphic = r;
						bestCount = componentCount;
					}
				}
			}
		}
		return graphic;
	}

	public float GetRadius() { return GetOwnSphere().radius; }

	public float GetSize() { return GetRadius()*2; }

	// TODO remove?
	public float GetEnergy() { return GetComponent<Agent_Properties> ().Energy; }

	// TODO have the player's camera implement a callback that does...
	// { inputController.cameraDistance = CalculateNewDistance(inputController.cameraDistance,oldSize,newSize); }
	public static float CalculateNewDistance(float currentDistance, float oldSize, float newSize) {
		return currentDistance * newSize / oldSize;
	}

	/// <summary>Sets the radius. NOTE: the radius should be HALF the size.</summary>
	/// <param name="radius">Radius.</param>
	public void SetRadius(float radius) {
		if(eatSphere && eatSphereRatio == 0) {
			eatSphereRatio = eatSphere.GetRadius() / GetOwnSphere().radius;
			eatSphereOffset = eatSphere.transform.localPosition;
		}
		if(GetMOB()) {
			mob.EnsureRigidBody();
		}
		float s = radius * 2;
		if(GetGraphic() == null || graphic.transform == transform) {
			if(transform.parent) { s /= transform.parent.lossyScale.z; }
			transform.localScale = new Vector3(s, s, s);
		} else {
			SphereCollider sc = GetComponent<SphereCollider>();
			if (onRadiusChange != null && sc.radius != radius) { onRadiusChange.Invoke (sc.radius, radius); }
			sc.radius = radius;
			transform.localScale = Vector3.one;
			graphic.transform.localScale = new Vector3(s, s, s);
		}
		TrailRenderer trail = transform.GetComponent<TrailRenderer>();
		if (trail) trail.startWidth = s;
		if (mob) {
			mob.rb.mass = s * Singleton.Get<GameRules>().massToEnergyRatio;
			if(GetGraphic() == null || graphic.transform == transform) {
				GetComponent<ParticleSystem>().SetParticleSize(1);
			} else {
				GetComponent<ParticleSystem>().SetParticleSize(s);
			}
		} else {
			GetComponent<ParticleSystem> ().SetParticleSize(s*5);
		}
		if(eatSphere) {
			eatSphere.SetRadius(radius*eatSphereRatio);
			eatSphere.transform.localPosition = eatSphereOffset * s;
		}
	}

//	public float CalculateSizeFromEnergy(float energy) {
//		float radius = (mob == null) ? Mathf.Sqrt (energy) : energy;
//		// TODO create mechanism to modify calculations for different types, including this calculation
//		radius *= Singleton.Get<GameRules> ().sizeToEnergyRatio;
//		radius += 1;
//		return radius;
//	}

//	public void SetSizeFromEnergy(float energy) {
//		SetRadius (CalculateSizeFromEnergy(energy)/2);
//	}

	public void SetEffectColor(Color c) {
		GetComponent<ParticleSystem>().SetColor(c);
		TrailRenderer tr = gameObject.GetComponent<TrailRenderer>();
		if (tr) {
			tr.material.SetColor("_TintColor", new Color(c.r, c.g, c.b, 0.25f));
		}
	}

	public void SetColor(Color c) {
		SetModelColor (c);
		SetEffectColor (c);
	}

	public void SetModelColor(Color c) {
		GetGraphic().material.color = c;
	}

	public Color GetColor() {
		return GetGraphic().material.color;
	}

	public Color GetEffectColor() {
		return GetComponent<ParticleSystem>().main.startColor.color;
	}

	public void SetParticleSpeed(float speed) {
		GetComponent<ParticleSystem> ().SetParticleSpeed (speed);
	}
}
