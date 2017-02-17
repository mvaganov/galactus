using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent_Properties : MonoBehaviour {

//	public delegate void ResourceChangeListener(Agent_Properties res, string resourceName, float oldValue, float newValue);

	public string typeName;

	[SerializeField]
	private Dictionary_string_float values = new Dictionary_string_float();
	private ValueCalculator<Agent_Properties> vc = null;
//	private TightBucketsOfUniques<string,ResourceChangeListener> changeListeners = null;

	void EnsureValueCalculations() {
		if (vc == null) {
			vc = new ValueCalculator<Agent_Properties> (this, values);
		}
	}

	void Start() {
		EnsureValueCalculations ();
	}

	public void Reset() {
		Dictionary_string_float defaults = Singleton.Get<GameRules> ().GetDefaultEnergyFor(typeName);
		Clear ();
		if (defaults != null) {
			foreach (KeyValuePair<string, float> current in defaults) {
				SetValue (current.Key, current.Value);
			}
		}
	}

	public float Energy { 
		get { return GetValue("energy"); }
		set { SetValue("energy", value); }
	}

	public float SpeedPts { get { return values["+speed"]; } }

	public float this[string i]
	{
		get { return vc.Get (i); } //return GetValue(i); }
		set { vc.Set (i, value); } //SetValue(i, value); }
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
//		if (changeListeners != null) {
//			float oldValue = 0;
//			if (!values.TryGetValue (resourceName, out oldValue)) {
//				oldValue = 0;
//			}
//			values [resourceName] = newValue;
//			changeListeners.ForEachUniqueInBucket (resourceName, thing => thing (this, resourceName, oldValue, newValue));
//			changeListeners.ForEachUniqueInBucket ("", thing => thing (this, resourceName, oldValue, newValue));
//		} else {
//			values [resourceName] = newValue;
//		}
		EnsureValueCalculations();
		vc.Set(resourceName, newValue);
	}

	public float GetValue(string resourceName) {
//		float amnt = 0;
//		if (values.TryGetValue (resourceName, out amnt)) {
//			return amnt;
//		}
//		return 0;
		return vc.Get(resourceName);
	}

	public void Clear() { values.Clear (); }

	public void AddValueChangeListener(ValueCalculator<Agent_Properties>.ChangeListener listener) {
		vc.AddValueChangeListener ("", listener);
	}

	/// <summary>Adds the value change listener.</summary>
	/// <param name="resourceName">Resource name. if an empty string, this listener will trigger for *every* change</param>
	/// <param name="listener">Listener.</param>
	public bool AddValueChangeListener(string resourceName, ValueCalculator<Agent_Properties>.ChangeListener listener) {
//		if (changeListeners == null) {
//			changeListeners = new TightBucketsOfUniques<string, ResourceChangeListener> (); //new Dictionary<string, ResourceChangeListener[]> ();
//		}
//		return changeListeners.AddUniqueBucketItem(resourceName, listener);
		EnsureValueCalculations();
		return vc.AddValueChangeListener(resourceName, listener);
	}

	public bool RemoveValueChangeListener(string resourceName, ValueCalculator<Agent_Properties>.ChangeListener listener) {
//		if (changeListeners != null) {
//			return changeListeners.RemoveUniqueBucketItem(resourceName, listener);
//		}
//		return false;
		return vc.RemoveValueChangeListener(resourceName, listener);
	}
}
