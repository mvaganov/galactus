using UnityEngine;
using System.Collections;

// TODO make an EnergyHeart or something for: resources, regular agents (minimal game logic), player agents (with the cool stat adjust stuff)
[RequireComponent(typeof(Agent_Properties))]
public class EnergyAgent : MonoBehaviour {

	protected float rad;
	protected float shortTimer;
	/// <summary>if non-zero, re-calculates energy drain as energy is lost</summary>
	protected float energyDrainPercentagePerSecondDuringMove;
	protected Agent_Properties resh;
	protected Agent_MOB mob;

//	public static ValueCalculator<Agent_Properties>.ChangeListener resizeAndReleaseWithEnergy = delegate(Agent_Properties res, string resourceName, float oldValue, float newValue) {
//		if(newValue <= 0) {
//			MemoryPoolItem.Destroy(res.gameObject);
//		} else {
//			Agent_SizeAndEffects asbe = res.GetComponent<Agent_SizeAndEffects>();
//			asbe.SetSizeFromEnergy(newValue);
//		}
//	};
//
//	// Use this for initialization
//	void Start () {
//		mob = GetComponent<Agent_MOB> ();
//		if (mob) { mob.EnsureRigidBody (); }
//		resh = GetComponent<Agent_Properties> ();
//		// add listener to the resource holder
//		resh.AddValueChangeListener ("energy", resizeAndReleaseWithEnergy);
//		energyDrainPercentagePerSecondDuringMove = Singleton.Get<GameRules> ().EnergyDrainPercentageFor(this);
//		float e = resh.Energy;
//		Agent_SizeAndEffects asbe = GetComponent<Agent_SizeAndEffects>();
//		asbe.SetSizeFromEnergy(e);
//	}
//
//	void FixedUpdate() {
//		if (mob && energyDrainPercentagePerSecondDuringMove != 0 && mob.rb.velocity != Vector3.zero) {
//			shortTimer += Time.deltaTime;
//			if (shortTimer >= 1) {
//				shortTimer -= 1;
//				float expectToDrain = rad * energyDrainPercentagePerSecondDuringMove;
//				float drained = resh.LoseValue ("energy", expectToDrain);
//				if (drained <= expectToDrain) return;
//			}
//		}
//	}
}
