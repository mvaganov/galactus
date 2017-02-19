using UnityEngine;
using System.Collections;

public class EatSphere : MonoBehaviour {
	[Tooltip("where resources will get funneled into")]
	public Agent_Properties owner;

	public string resourceName;
	public float power = 1;
	public float conversionRate = .5f;

	private Material m;

	public float cooldown = .75f;
	[Tooltip("After the warmup, how long can the 'ready' state be kept before activation is lost?")]
	public float holdDuringActivate = .25f;
	public float warmup = .25f;
	public string warmupEffect, activateEffect, cooledEffect;
	private Effects.Effect effect_warmup;
	private Effects.Effect effect_activate;
	private Effects.Effect effect_cooled;
	/// <summary>what to set a held eat-sphere transparency to, to reduce obstruction of visibility</summary>
	[HideInInspector]
	public float holdTransparency = 1;

	private float waiting = 0;
	private enum WaitingFor { nothing, warmup, activate, cooldown };
	private WaitingFor waitState = WaitingFor.nothing;

	public bool autoActivateResource = true, autoActivateAgent = true, maintainActivation = false;

	public Agent_SizeAndEffects eating;

	public float GetRadius() { return transform.lossyScale.z/2; }
	public void SetRadius(float rad) {
		float s = rad * 2;
		//Transform p = transform.parent;
		//transform.parent = null;
		transform.localScale = new Vector3 (s, s, s);
		//transform.parent = p;
	}

	void Start() {
		m = GetComponent<Renderer> ().material;
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
		SetSphereVisual (0);
	}

	void FixedUpdate () {
		if(Input.GetKeyDown(KeyCode.Alpha7)) {
			owner.AddToValue (resourceName, 1);
		}
		if (waiting > 0) { 
			waiting -= Time.deltaTime;
			if (waiting <= 0) {
				switch (waitState) {
				case WaitingFor.warmup:
					waiting += holdDuringActivate;
					waitState = WaitingFor.activate;
					break;
				case WaitingFor.activate:
					waiting = cooldown;
					waitState = WaitingFor.cooldown;
					break;
				case WaitingFor.cooldown:
					waitState = WaitingFor.nothing;
					if (effect_cooled != null) {
						effect_cooled.Emit (5, transform.position, transform);
					}
					break;
				}
			} else {
				switch (waitState) {
				case WaitingFor.warmup:
					SetSphereVisual ((warmup - Mathf.Max(0,waiting)) / warmup);
					break;
				case WaitingFor.activate:
					SetSphereVisual (maintainActivation?holdTransparency:1);
					break;
				case WaitingFor.cooldown:
					SetSphereVisual (waiting / cooldown);
					break;
				}
			}
			// because an eaten object doesnt go away (it's just hidden by the memory pool), this is how we know we've stopped eating.
			if (eating && !eating.gameObject.activeInHierarchy) { eating = null; }
		}
		if (maintainActivation) { Activate (); }
	}

	public bool IsActive() { return waitState != WaitingFor.nothing; }

	public void Activate() {
		switch (waitState) {
		case WaitingFor.nothing:
			if (waitState == WaitingFor.nothing) {
				waiting = warmup;
				waitState = WaitingFor.warmup;
				if (effect_warmup != null) {
					effect_warmup.Emit (5, transform.position, transform);
				}
			}
			break;
		case WaitingFor.activate:
			waiting = holdDuringActivate;
			break;
		}
	}

	public Agent_SizeAndEffects GetMeal() { return eating; }

	void OnTriggerExit(Collider c) {
		eating = null;
	}

	void SetSphereVisual(float percent) {
		if (percent == 0) {
			GetComponent<Renderer> ().enabled = false;
		}
		Color c = m.GetColor("_TintColor");
		if (c.a == 0 && percent != 0) {
			GetComponent<Renderer> ().enabled = true;
		}
		if (c.a != percent) {
			m.SetColor("_TintColor", new Color (c.r, c.g, c.b, percent));
		}
	}

	void OnTriggerStay(Collider c) {
		Agent_SizeAndEffects caught = c.gameObject.GetComponent<Agent_SizeAndEffects> ();
		if (!caught) return;
		eating = caught;
		switch (waitState) {
		case WaitingFor.nothing:
			if (autoActivateResource && !caught.GetEatSphere()
				|| autoActivateAgent && caught.GetEatSphere()) {
				if (warmup == 0) {
					waitState = WaitingFor.activate;
					OnTriggerStay (c);
					return;
				}
				Activate ();
			}
			break;
		case WaitingFor.activate:
			if (caught && caught != owner) {
				Agent_Properties other = caught.GetComponent<Agent_Properties> ();
				float effectivePower = Mathf.Max(0,power - other ["defense"]);
				float whatIsLeft = other.LoseValue (resourceName, effectivePower);
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
