#define PREVENT_STACK_OVERFLOW
#define SHOW_DEPENDENCIES
#define FAIL_ON_CALCULATED_VALUE_ASSIGNMENT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ValueCalculator<TARGET> {

	[HideInInspector]
	public TARGET target;

	public delegate T ValueRules<T>(TARGET a);
	public delegate T Adjustment<T>(T originalValue, object modifyingValue);
	public delegate void ChangeListener(TARGET res, string resourceName, float oldValue, float newValue);

	/// <summary>The values being managed. if there is no entry, but there is a calculations function, a value will be 
	/// created based on the calculation function</summary>
	//public Dictionary<string,float> values = new Dictionary<string, float> ();
	[SerializeField]
	private Dictionary_string_float values;// = new Dictionary_string_float();
	private TightBucketsOfUniques<string,ChangeListener> changeListeners = null;

	/// <summary>how to calculate values</summary>
	public Dictionary<string,ValueRules<float>> valueCalculationRules = new Dictionary<string, ValueRules<float>>();
	/// <summary>identify which values have an impact on which other values. If a Key value is changed, invalidate each of the Value[] values</summary>
	private TightBucketsOfUniques<string, string> valueDependencies = null;

	/// <summary>which value is being watched --- FINISHME</summary>
	private string watchingDependency = null;
	#if PREVENT_STACK_OVERFLOW
	private List<string> dependenciesBeingCalculated = new List<string>();
	#endif
	/// <summary>modifiers for variables, which are applied after variables are calculated</summary>
	public TightBucketsOfUniques<string,Modifier<float>> modifiers = null;

	/// <param name="target">Target. an object passed to each calculation function, likely relevant to the values stored in this structure.</param>
	public ValueCalculator(TARGET target, Dictionary_string_float dict) {
		this.target = target;
		this.values = dict;
	}

	public void SetValueRules(Dictionary<string,ValueRules<float>> rules) {
		valueCalculationRules = rules;
	}

	void CalculateValueDependency(string valueName) {
		watchingDependency = valueName;
		Get (valueName);
		watchingDependency = null;
	}

	// TODO only calculate value dependency table once, and store that in a static place, reference-able by other Agent_Adjust of the same type...
	public TightBucketsOfUniques<string, string> CalculateValueDependencies() {
		valueDependencies = new TightBucketsOfUniques<string, string> ();
		foreach (var kvp in valueCalculationRules) {
			CalculateValueDependency (kvp.Key);
		}
		#if SHOW_DEPENDENCIES
		string dependencyChart = "";
		foreach(var d in valueDependencies) {
			string str = "[";
			if(d.Value != null) {
				for(int i=0;i<d.Value.Length;++i){
					if(i>0)str+=", ";
					str+=d.Value[i];
				}
			}
			str+="]";
			dependencyChart += ((dependencyChart.Length > 0)?"\n":"") + d.Key + ": " + str;
		}
		Debug.Log (dependencyChart);
		#endif
		return valueDependencies;
	}

	public struct Modifier<T> {
		public string name;
		public object value;
		public ValueCalculator<TARGET>.Adjustment<T> adjustment;
		public T Modify(T value) {
			return adjustment (value, this.value);
		}
		public Modifier(string name, ValueCalculator<TARGET>.Adjustment<T> adjustment, object value){
			this.name = name;
			this.value = value;
			this.adjustment = adjustment;
		}
		public Modifier(string name, ValueCalculator<TARGET>.Adjustment<T> adjustment) {
			this.name = name;
			this.value = null;
			this.adjustment = adjustment;
		}
		public override int GetHashCode () {
			return base.GetHashCode ()+adjustment.GetHashCode()+value.GetHashCode()+name.GetHashCode();
		}
		public override bool Equals(System.Object obj) {
			if(obj is Modifier<T>) {
				Modifier<T> m = (Modifier<T>)obj;
				return name == m.name && value == m.value && adjustment == m.adjustment;
			}
			return false;
		}
		public static bool operator== (ValueCalculator<TARGET>.Modifier<T> a, ValueCalculator<TARGET>.Modifier<T> b) {
			if (System.Object.ReferenceEquals(a, b)) { return true; }
			if (((object)a == null) || ((object)b == null)) { return false; }
			return a.Equals (b);
		}
		public static bool operator !=(ValueCalculator<TARGET>.Modifier<T> a, ValueCalculator<TARGET>.Modifier<T> b) { return !(a == b); }
	}

	public static Adjustment<float> addition = delegate(float originalValue, object modifyingValue) {
		return originalValue + (float)modifyingValue;
	};
	public static Adjustment<float> multiplication = delegate(float originalValue, object modifyingValue) {
		return originalValue * (float)modifyingValue;
	};

	public bool AddValueModifier(string valueName, Modifier<float> modifier) {
		if (modifiers == null) {
			modifiers = new TightBucketsOfUniques<string, Modifier<float>>();
		}
		return modifiers.AddUniqueBucketItem(valueName, modifier);
	}

	public bool RemoveValueModifier(string valueName, Modifier<float> modifier) {
		return modifiers != null && modifiers.RemoveUniqueBucketItem(valueName, modifier);
	}

	public Dictionary<string, Modifier<float>[]> GetModifiers() {
		return modifiers;
	}

	public float Calculate(string valueName, float startingPoint) {
		#if PREVENT_STACK_OVERFLOW
		if(dependenciesBeingCalculated.Contains(valueName)) {
			throw new UnityException("dependency stack recursion: "+dependenciesBeingCalculated+" "+valueName);
		}
		dependenciesBeingCalculated.Add(valueName);
		#endif
//		if (valueDependencies != null && watchingDependency != null && watchingDependency != valueName) {
//			valueDependencies.AddUniqueBucketItem (valueName, watchingDependency);
//		}
		float result = startingPoint;
		if (modifiers != null) {
			modifiers.ForEachUniqueInBucket (valueName, (m) => {
				result = m.Modify (result);
			});
		}
		#if PREVENT_STACK_OVERFLOW
		if(dependenciesBeingCalculated[dependenciesBeingCalculated.Count-1] != valueName) {
			throw new UnityException("dependency stack ejection problem: "+dependenciesBeingCalculated+" "+valueName);
		}
		dependenciesBeingCalculated.RemoveAt(dependenciesBeingCalculated.Count-1);
		#endif
		if (valueDependencies != null) {
			// invalidate values that depend on this one, which will cause them to re-calculate next time they are checked.
			valueDependencies.ForEachUniqueInBucket (valueName, (dependency) => { InvalidateCache (dependency); });
		}
		return result;
	}

	public void InvalidateCache(string valueName) {
		values.Remove (valueName);
	}

	public void InvalidateAllCalculatedValues() {
		foreach(var kvp in valueCalculationRules) {
			if (kvp.Value != null) {
				values.Remove (kvp.Key);
			}
		}
	}

	public float this[string i] {
		get { return Get(i); }
		set { Set(i, value); }
	}

	public float Get(string valueName) {
		// if we need this value while we're looking for another (watched) value
		if (valueDependencies != null && watchingDependency != null && watchingDependency != valueName) {
			// mark that this value is a dependency of the watched value
			valueDependencies.AddUniqueBucketItem (valueName, watchingDependency);
		}
		float result;
		if (!values.TryGetValue (valueName, out result) || watchingDependency != null) {
			ValueRules<float> initFunc;
			if (valueCalculationRules.TryGetValue (valueName, out initFunc)) {
				result = Calculate (valueName, initFunc (target));
				values [valueName] = result; // NOTE: explicitly do not trigger changeListeners. that would be madness.
			}
		}
		return result;
	}

	public void Set(string valueName, float newValue) {
		#if FAIL_ON_CALCULATED_VALUE_ASSIGNMENT
		// if this value is calculated, fail.
		ValueRules<float> initFunc;
		if (valueCalculationRules.TryGetValue (valueName, out initFunc)) {
			throw new UnityException ("Cannot assign to automatically-calculated value \'"+valueName+"\'");
		}
		#endif
		if (changeListeners != null) {		// if there are change listeners
			float oldValue = 0;
			if (!values.TryGetValue (valueName, out oldValue)) {
				oldValue = 0;
			}
			values [valueName] = newValue;
			changeListeners.ForEachUniqueInBucket (valueName, thing => thing (target, valueName, oldValue, newValue));
			changeListeners.ForEachUniqueInBucket ("", thing => thing (target, valueName, oldValue, newValue));
		} else {
			values [valueName] = newValue;
		}
		if (valueDependencies != null) { // if other values might depend on this one
			// invalidate values that depend on this one, which will cause them to re-calculate next time they are checked.
			valueDependencies.ForEachUniqueInBucket (valueName, (dependency) => { InvalidateCache (dependency); });
		}
	}

	public void AddValueChangeListener(ChangeListener listener) { AddValueChangeListener ("", listener); }

	/// <summary>Adds the value change listener.</summary>
	/// <param name="resourceName">Resource name. if an empty string, this listener will trigger for *every* change</param>
	/// <param name="listener">Listener.</param>
	public bool AddValueChangeListener(string resourceName, ChangeListener listener) {
		if (changeListeners == null) {
			changeListeners = new TightBucketsOfUniques<string, ChangeListener> ();
		}
		return changeListeners.AddUniqueBucketItem(resourceName, listener);
	}

	public bool RemoveValueChangeListener(string resourceName, ChangeListener listener) {
		return (changeListeners != null) ? changeListeners.RemoveUniqueBucketItem(resourceName, listener) : false;
	}
}
