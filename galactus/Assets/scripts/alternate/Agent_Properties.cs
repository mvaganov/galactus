using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent_Properties : MonoBehaviour {
	public string typeName;

	[HideInInspector]
	public Agent_MOB mob;
	[HideInInspector]
	public Agent_SizeAndEffects sizeNeffects;
	[HideInInspector]
	public EatSphere eatS;

	public EatSphere GetEatSphere() { return eatS; }
	public Agent_SizeAndEffects GetSizeAndEffects() { return sizeNeffects; }
	public Agent_MOB GetMOB() { return mob; }
	public float GetRadius() { return sizeNeffects.GetRadius (); }
	public float GetSize() { return sizeNeffects.GetSize (); }

	[SerializeField]
	private Dictionary_string_float values = new Dictionary_string_float();
	private ValueCalculator<Agent_Properties> vc = null;

	public string DEBUG_OUT() {
		return vc.DEBUG_GetDependenciesBeingCalculated ();
	}

	void EnsureValueCalculations() {
		if (vc == null) {
			// if there are rules for this sort of thing
			GameRules.ValueRules vr;
			if (GameRules.AGENT_RULES.TryGetValue (typeName, out vr)) {
				// prepare to implement those rules
				vc = new ValueCalculator<Agent_Properties> (this, values);
//				print ("getting rules for " + typeName);
				// apply those rules to this thing!
				vc.SetValueRules (vr.calculation, vr.changeListeners, vr.dependencies);
				// if the dependencies havent been calculated for these rules yet
				if (vr.dependencies == null) {
					// use the dependency calculation just done here as the standard for the rule set
					vr.dependencies = vc.GetDependencies ();
				}
			} else {
				Debug.LogError ("MISSING RULES FOR TYPE \'"+typeName+"\'");
			}
		}
	}

	public bool HasValue(string valueName) {
		return vc.HasValue (valueName);
	}

	public float GetCached(string valueName) {
		return vc.GetCached(valueName);
	}

	public void EnsureComponents() {
		if (!mob) {
			mob = GetComponent<Agent_MOB> ();
			sizeNeffects = GetComponent<Agent_SizeAndEffects> ();
			eatS = sizeNeffects.GetEatSphere ();
		}
		EnsureValueCalculations ();
	}

	void Start() {
		EnsureComponents ();
	}

	float energyDrainTimer = 1;
	void FixedUpdate() {
		energyDrainTimer -= Time.deltaTime;
		if (energyDrainTimer <= 0) {
			energyDrainTimer += 1;
			LoseValue ("energy", this ["energyDrain"]);
		}
	}

	public void Reset() {
		EnsureComponents ();
		Dictionary_string_float defaults = Singleton.Get<GameRules> ().GetDefaultEnergyFor(typeName);
		Clear ();
		if (defaults != null) {
			foreach (KeyValuePair<string, float> current in defaults) {
				SetValue (current.Key, current.Value);
			}
		}
		// TODO if this agent has an Agent_TargetFinder, reset the AI to .none
		Agent_TargetFinder tf = GetComponent<Agent_TargetFinder>();
		if (tf) {
			tf.Reset ();
		}
	}

	public float Energy { 
		get { return GetValue("energy"); }
		set { SetValue("energy", value); }
	}

	public float this[string i]
	{
		get { return vc.Get (i); }
		set { vc.Set (i, value); }
	}

	public float LoseValue(string name, float amountTaken) {
		float whatIsLeft;
		if (values.TryGetValue (name, out whatIsLeft)) {
			if (whatIsLeft >= amountTaken) {
				whatIsLeft -= amountTaken;
				SetValue (name, whatIsLeft);
				return amountTaken;
			}
			SetValue (name, 0);
			return whatIsLeft;
		}
		return 0;
	}

	public Dictionary_string_float GetProperties() {
		return values;
	}

	public void AddToValue(string resourceName, float amountToAdd) {
		if (amountToAdd != 0) { 
			float current = 0;
			if (!values.TryGetValue (resourceName, out current)) { current = 0; }
			SetValue (resourceName, current + amountToAdd);
		}
	}

	public void SetValue(string resourceName, float newValue) {
		EnsureValueCalculations();
		if (vc != null) {
			vc.Set (resourceName, newValue);
		} else {
			print ("missing VC for "+typeName+" "+name);
			values [resourceName] = newValue;
		}
	}

	public float GetValue(string resourceName) {
		if (vc != null) {
			return vc.Get (resourceName);
		}
		print ("missing VC for "+typeName+" "+name);
		float valu;
		return values.TryGetValue (resourceName, out valu) ? valu : 0;
	}

	public void Clear() { values.Clear (); }

	public void AddValueChangeListener(ValueCalculator<Agent_Properties>.ChangeListener listener) {
		vc.AddValueChangeListener ("", listener);
	}

	/// <summary>Adds the value change listener.</summary>
	/// <param name="resourceName">Resource name. if an empty string, this listener will trigger for *every* change</param>
	/// <param name="listener">Listener.</param>
	public bool AddValueChangeListener(string resourceName, ValueCalculator<Agent_Properties>.ChangeListener listener) {
		EnsureComponents ();
		if (vc != null) {
			return vc.AddValueChangeListener (resourceName, listener);
		} else {
			return false;
		}
	}

	public bool RemoveValueChangeListener(string resourceName, ValueCalculator<Agent_Properties>.ChangeListener listener) {
		return vc.RemoveValueChangeListener(resourceName, listener);
	}

	public bool AI_IsDangerous() { return eatS != null; }

	public float AI_CalculateDangerEstimate() { return this ["eatPower"]; }

	/// <returns>negative for how badly I would be beaten, positive for how handily I would win. 0 for unknown.</returns>
	/// <param name="him">Him.</param>
	public float AI_CalculateHowIWouldDoInAFightAgainst(Agent_Properties him) {
		EatSphere mine = GetEatSphere (), his = him.GetEatSphere();
		// TODO factor in agent speed, turn speed, range, and compare that to this agent
		float hisDamage = his?his.GetRadius():0;
		float myDamage = mine?mine.GetRadius ():0;
		return myDamage - hisDamage;
	}
}
