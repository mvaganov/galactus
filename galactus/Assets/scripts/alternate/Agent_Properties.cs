using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent_Properties : MonoBehaviour {

	public delegate void ResourceChangeListener(Agent_Properties res, string resourceName, float oldValue, float newValue);

	public string typeName;

	[SerializeField]
	private Dictionary_string_float numericValues = new Dictionary_string_float();
	private Dictionary<string,ResourceChangeListener[]> changeListeners = null;

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
		get { return numericValues["energy"]; }
		set { SetValue("energy", value); }
	}

	public float this[string i]
	{
		get { return numericValues[i]; }
		set { SetValue(i, value); }
	}

	public float LoseValue(string name, float amountTaken) {
		float whatIsLeft;
		if (numericValues.TryGetValue (name, out whatIsLeft)) {
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

	public Dictionary_string_float GetValues() {
		return numericValues;
	}

	public void AddToValue(string resourceName, float amountToAdd) {
		if (amountToAdd != 0) { 
			float current = 0;
			if (!numericValues.TryGetValue (resourceName, out current)) { current = 0; }
			SetValue (resourceName, current + amountToAdd);
		}
	}

	public void SetValue(string resourceName, float newValue) {
		if (changeListeners != null) {
			float oldValue = 0;
			if (!numericValues.TryGetValue (resourceName, out oldValue)) {
				oldValue = 0;
			}
			numericValues [resourceName] = newValue;
			ResourceChangeListener[] listeners;
			if (changeListeners.TryGetValue (resourceName, out listeners)) {
				for (int i = 0; i < listeners.Length; ++i) {
					listeners [i] (this, resourceName, oldValue, newValue);
				}
			}
		} else {
			numericValues [resourceName] = newValue;
		}
	}

	public float GetValue(string resourceName) {
		float amnt = 0;
		if (numericValues.TryGetValue (resourceName, out amnt)) {
			return amnt;
		}
		return 0;
	}

	public void Clear() {
		numericValues.Clear ();
	}

	public void AddValueChangeListener(string resourceName, ResourceChangeListener listener) {
		ResourceChangeListener[] listeners;
		if (changeListeners == null) {
			changeListeners = new Dictionary<string, ResourceChangeListener[]> ();
		}
		if (changeListeners.TryGetValue (resourceName, out listeners)) {
			System.Array.Resize (ref listeners, listeners.Length + 1);
			listeners [listeners.Length - 1] = listener;
		} else {
			listeners = new ResourceChangeListener[1]{listener};
		}
		changeListeners [resourceName] = listeners;
	}
}
