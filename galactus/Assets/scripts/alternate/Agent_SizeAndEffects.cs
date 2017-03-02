using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent_SizeAndEffects : MonoBehaviour {

	[SerializeField]
	private EatSphere eatSphere;
	public EatSphere GetEatSphere() { return eatSphere; }
	private Agent_MOB mob;

	void Start() {
		mob = GetComponent<Agent_MOB> ();
	}

	public float GetRadius() {
		return transform.lossyScale.z/2;
	}
	public float GetSize() {
		return transform.lossyScale.z;
	}

	// TODO remove?
	public float GetEnergy() {
		return GetComponent<Agent_Properties> ().Energy;
	}

	/// <summary>Sets the radius. NOTE: the radius should be HALF the size.</summary>
	/// <param name="radius">Radius.</param>
	public void SetRadius(float radius) {
		float s = radius * 2;
		if (transform.parent) { s /= transform.parent.lossyScale.z; }
		transform.localScale = new Vector3 (s,s,s);
		TrailRenderer trail = transform.GetComponent<TrailRenderer>();
		if (trail) trail.startWidth = s;
		if (mob) {
			mob.rb.mass = s * Singleton.Get<GameRules>().massToEnergyRatio;
			GetComponent<ParticleSystem> ().SetParticleSize(1);
		} else {
			GetComponent<ParticleSystem> ().SetParticleSize(s*5);
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

	public void SetModelColor(Color c) {
		GetComponent<Renderer> ().material.color = c;
	}

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

	public Color GetColor() {
		return GetComponent<Renderer> ().material.color;
	}

	public Color GetEffectColor() {
		return GetComponent<ParticleSystem>().main.startColor.color;
	}

	public void SetParticleSpeed(float speed) {
		GetComponent<ParticleSystem> ().SetParticleSpeed (speed);
	}
}
