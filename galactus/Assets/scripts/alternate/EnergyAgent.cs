using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Agent_Properties))]
public class EnergyAgent : MonoBehaviour {

	protected float rad;
	protected float shortTimer;
	/// <summary>if non-zero, re-calculates energy drain as energy is lost</summary>
	protected float energyDrainPercentagePerSecondDuringMove;
	protected Agent_Properties resh;
	protected Agent_MOB mob;

	[SerializeField]
	private EatSphere eatSphere;
	public EatSphere GetEatSphere() { return eatSphere; }

	public float GetRadius() {
		return rad;
	}

	public float GetEnergy() {
		return GetComponent<Agent_Properties> ().Energy;
	}

	public static Agent_Properties.ResourceChangeListener resizeAndReleaseWithEnergy = delegate(Agent_Properties res, string resourceName, float oldValue, float newValue) {
		if(newValue <= 0) {
			MemoryPoolItem.Destroy(res.gameObject);
		} else {
			EnergyAgent asbe = res.GetComponent<EnergyAgent>();
			asbe.SetSize(newValue);
		}
	};

	// Use this for initialization
	void Start () {
		mob = GetComponent<Agent_MOB> ();
		if (mob) { mob.EnsureRigidBody (); }
		resh = GetComponent<Agent_Properties> ();
		// add listener to the resource holder
		resh.AddValueChangeListener ("energy", resizeAndReleaseWithEnergy);
		energyDrainPercentagePerSecondDuringMove = Singleton.Get<GameRules> ().EnergyDrainPercentageFor(this);
		float e = resh.Energy;
		SetSize (e);
	}

	void FixedUpdate() {
		if (mob && energyDrainPercentagePerSecondDuringMove != 0 && mob.rb.velocity != Vector3.zero) {
			shortTimer += Time.deltaTime;
			if (shortTimer >= 1) {
				shortTimer -= 1;
				float expectToDrain = rad * energyDrainPercentagePerSecondDuringMove;
				float drained = resh.LoseValue ("energy", expectToDrain);
				if (drained <= expectToDrain) return;
			}
		}
	}

	void SetRadius(float radius) {
		this.rad = radius;
		transform.localScale = new Vector3 (rad, rad, rad);
		TrailRenderer trail = transform.GetComponent<TrailRenderer>();
		if (trail) trail.startWidth = rad;
		if (mob) {
			mob.rb.mass = radius;
		} else {
			GetComponent<ParticleSystem>().startSize = radius*10;
		}

	}
	public float CalculateRadiusFromEnergy(float energy) {
		float radius = (mob == null) ? Mathf.Sqrt (energy) : energy;
		// TODO create mechanism to modify calculations for different types, including this calculation
		radius *= Singleton.Get<GameRules> ().sizeToEnergyRatio;
		radius += 1;
		return radius;
	}

	void SetSize(float energy) {
		SetRadius (CalculateRadiusFromEnergy(energy));
	}

	public void SetColor(Color c) {
		ParticleSystem ps = GetComponent<ParticleSystem>();
		ps.startColor = c;
		TrailRenderer tr = gameObject.GetComponent<TrailRenderer>();
		if (tr) {
			tr.material.SetColor("_TintColor", new Color(c.r, c.g, c.b, 0.25f));
		}
	}

	public Color GetColor() {
		return GetComponent<ParticleSystem>().startColor;
	}
}
