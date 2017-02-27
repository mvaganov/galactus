using UnityEngine;
using System.Collections;

public class EatSphere : MonoBehaviour {
	[Tooltip("where resources will get funneled into")]
	public Agent_Properties owner;
	Collider ownerCollider;

	public string resourceName;
	public float conversionRate = .5f;

	private Material m;

	public float cooldown = .75f;
	[Tooltip("After the warmup, how long can the 'ready' state be kept before activation is lost?")]
	public float holdDuringActivate = .25f;
	public float warmup = .25f;
	public string warmupEffect, activateEffect, cooledEffect;
	// TODO make these in some kind of settings class, accessible statically.
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

	public Agent_Properties eating;

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
		ownerCollider = owner.GetComponent<Collider> ();
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

	public Agent_Properties GetMeal() { return eating; }

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

	Collider lastCollided = null;

	void OnTriggerStay(Collider c) {
		if (c == ownerCollider) return;
		if (c != lastCollided) {
			lastCollided = c;
			eating = lastCollided.gameObject.GetComponent<Agent_Properties> ();
		}
		if (!eating) return;
		switch (waitState) {
		case WaitingFor.nothing:
			if (autoActivateResource && !eating.GetEatSphere()
				|| autoActivateAgent && eating.GetEatSphere()) {
				if (warmup == 0) {
					waitState = WaitingFor.activate;
					OnTriggerStay (c);
					return;
				}
				Activate ();
			}
			break;
		case WaitingFor.activate:
			if (eating && eating != owner) {
				float otherE = eating [resourceName];
				if (otherE != 0) {
					float otherD = eating ["defense"];
					otherD = Mathf.Max(0, otherD-owner["penetration"]);
					float effectivePower = Mathf.Max(0,owner["eatPower"] - otherD);
					bool willBeAliveAfterDrain = otherE > effectivePower;
					float drained = (willBeAliveAfterDrain)?effectivePower:otherE;
					Vector3 u = Camera.main.transform.up;
					FloatyText ft = Effects.FloatyText(eating.transform.position, transform, "-energy ("+drained.ToString("#.#")+")", Color.red, -2);
					ParticleDrain pd = Effects.ParticleDrain (eating.transform, transform, eating.GetColor (), (int)(drained * 10), owner ["speed"]*2);
					if (willBeAliveAfterDrain) {
						ft.transform.SetParent (eating.transform);
					} else {
						pd.transform.SetParent (null);
					}
					float gained = drained * conversionRate;
					Effects.FloatyText(transform.position+u, transform, "+energy ("+gained.ToString("#.#")+")", Color.white);
					//print ("draining " + caught + " " + whatIsLeft + " " + resourceName);
					owner.AddToValue (resourceName, gained);
					if (effect_activate != null) {
						effect_activate.ps.SetColor (owner.GetEffectColor ());
						effect_activate.Emit ((int)(drained * 10), transform.position, transform);
					}
					// drain at the end, because this method might cause the eaten object to go away
					eating.LoseValue (resourceName, drained);
				}
				waiting = cooldown;
				waitState = WaitingFor.cooldown;
			}
			break;
		}
	}
}
