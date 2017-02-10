using UnityEngine;
using System.Collections;

public class EatSphere : MonoBehaviour {
	[Tooltip("where resources will get funneled into")]
	public Agent_Properties owner;

	public string resourceName;
	public float amount = 1;
	public float conversionRate = .5f;

	public float cooldown = .75f;
	[Tooltip("After the warmup, how long can the 'ready' state be kept before activation is lost?")]
	public float holdDuringActivate = 1;
	public float warmup = .25f;
	public string warmupEffect, activateEffect, cooledEffect;
	private Effects.Effect effect_warmup;
	private Effects.Effect effect_activate;
	private Effects.Effect effect_cooled;

	private float waiting = 0;
	private enum WaitingFor { nothing, warmup, activate, cooldown };
	private WaitingFor waitState = WaitingFor.nothing;

	public Agent_Properties eating;

	void Start() {
		if (owner == null) {
			Transform t = transform;
			do {
				owner = t.GetComponent<Agent_Properties> ();
				t = t.parent;
			} while(t != null && owner == null);
		}
		Effects e = Singleton.Get<Effects> ();
		effect_warmup = e.Get (warmupEffect);
		effect_activate = e.Get (activateEffect);
		effect_cooled = e.Get (cooledEffect);
	}

	void FixedUpdate () {
		if (waiting > 0) { 
			waiting -= Time.deltaTime;
			if(waiting <= 0) {
				switch (waitState) {
				case WaitingFor.warmup:
					waiting += holdDuringActivate;
					waitState = WaitingFor.activate; break;
				case WaitingFor.activate:
					waiting = cooldown;
					waitState = WaitingFor.cooldown; break;
				case WaitingFor.cooldown:
					waitState = WaitingFor.nothing;
					if (effect_cooled != null) {
						effect_cooled.Emit (5, transform.position, transform);
					}
					break;
				}
			}
			// because an eaten object doesnt go away (it's just hidden by the memory pool), this is how we know we've stopped eating.
			if (eating && !eating.gameObject.activeInHierarchy) { eating = null; }
		}
	}

	public void StartActivation() {
		if (waitState == WaitingFor.nothing) {
			waiting = warmup;
			waitState = WaitingFor.warmup;
			if (effect_warmup != null) {
				effect_warmup.Emit (5, transform.position, transform);
			}
		}
	}

	public Agent_Properties GetMeal() { return eating; }

	void OnTriggerExit(Collider c) {
		eating = null;
	}

	void OnTriggerStay(Collider c) {
		Agent_Properties caught = c.gameObject.GetComponent<Agent_Properties> ();
		if (!caught) return;
		eating = caught;
		switch (waitState) {
		case WaitingFor.nothing:
			if (warmup == 0) {
				waitState = WaitingFor.activate;
				OnTriggerStay (c);
				return;
			}
			StartActivation ();
			break;
		case WaitingFor.activate:
			if (caught && caught != owner) {
				float whatIsLeft = caught.LoseValue (resourceName, amount);
				if (whatIsLeft != 0) {
					//print ("draining " + caught + " " + whatIsLeft + " " + resourceName);
					owner.AddToValue (resourceName, whatIsLeft * conversionRate);
					if (effect_activate != null) {
						effect_activate.Emit ((int)(whatIsLeft * 10), transform.position, transform);
					}
				}
				waiting = cooldown;
				waitState = WaitingFor.cooldown;
			}
			break;
		}
	}
}
