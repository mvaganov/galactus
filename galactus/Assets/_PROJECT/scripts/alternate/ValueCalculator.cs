#define PREVENT_STACK_OVERFLOW
#define FAIL_ON_CALCULATED_VALUE_ASSIGNMENT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An object of this class wraps around another object and allows callbacks to monitor when it's float variables are 
/// modified, with the expectation that those callbacks could be modifying other values
/// </summary>
[System.Serializable]
public class ValueCalculator<TARGET> {

	[HideInInspector]
	public TARGET target;

	public delegate T ValueCalculation<T>(TARGET a);
	public delegate T Adjustment<T>(T originalValue, object modifyingValue);
	public delegate void ChangeListener(TARGET res, string resourceName, float oldValue, float newValue);

	/// <summary>The values being managed. if there is no entry, but there is a calculations function, a value will be 
	/// created based on the calculation function</summary>
	//public Dictionary<string,float> values = new Dictionary<string, float> ();
	[SerializeField]
	private Dictionary_string_float values;
	/// <summary>The change listeners, which should be added and removed freely</summary>
	private TightBucketsOfUniques<string,ChangeListener> changeListeners = null;
	/// <summary>The static change listeners, which should really only be set one for a type of class</summary>
	private Dictionary<string,ChangeListener> staticChangeListeners = null;

	private System.Collections.Generic.HashSet<string> needsCalculation = new HashSet<string>(); 

	/// <summary>how to calculate values</summary>
	public Dictionary<string,ValueCalculation<float>> valueCalculationRules = new Dictionary<string, ValueCalculation<float>>();
	/// <summary>identify which values have an impact on which other values. If a Key value is changed, invalidate each of the Value[] values</summary>
	private TightBucketsOfUniques<string, string> valueDependencies = null;

	/// <summary>which value is being watched --- FINISHME</summary>
	private string watchingDependency = null;
	private int dependencyDepth = -1;
	#if PREVENT_STACK_OVERFLOW
	private List<string> dependenciesBeingCalculated = new List<string>();
	#endif
	// TODO is this needed? should Modifiers be removed? this feature may be useful in the future... ?
	/// <summary>modifiers for variables, which are applied after variables are calculated</summary>
	public TightBucketsOfUniques<string,Modifier<float>> modifiers = null;

	public string PrintDependenciesBeingCalculated() {
		#if PREVENT_STACK_OVERFLOW
		string s = "";
		for(int i = 0; i < dependenciesBeingCalculated.Count; ++i) {
			if(i > 0) s += ", ";
			s += dependenciesBeingCalculated.ToString();
		}
		return s;
		#else
		return "only available when PREVENT_STACK_OVERFLOW is set";
		#endif
	}

	/// <param name="target">Target. an object passed to each calculation function, likely relevant to the values stored in this structure.</param>
	/// <param name="dict">the dictionary store to use for storing values. will be heavily modified.</param>
	/// <param name="staticRuleset">an optional dictionary of ChangeListeners, which will not be modified by this structure</param> 
	public ValueCalculator(TARGET target, Dictionary_string_float dict) {
		this.target = target;
		this.values = dict;
	}
	/// <returns><c>true</c> if this instance has the specified value; otherwise, <c>false</c>.</returns>
	public bool HasValue(string valueName) { return values.ContainsKey (valueName); }

	/// <returns>The cached value for the given value name.</returns>
	public float GetCached(string valueName) { return values [valueName]; }

	/// <summary>Sets the value rules.</summary>
	/// <param name="calculationRules">Calculation rules. how each automatically-calculated variable is calculated</param>
	/// <param name="staticChangeListeners">Static change listeners. an immutable list of how variables should notify when calculated/changed</param>
	/// <param name="valueDependencies">Value dependencies. a table of which values are dependencies for which other variables</param>
	public void SetValueRules(
		Dictionary<string,ValueCalculation<float>> calculationRules,
		Dictionary<string,ChangeListener> staticChangeListeners,
		TightBucketsOfUniques<string, string> valueDependencies
	) {
		this.valueCalculationRules = calculationRules;
		this.staticChangeListeners = staticChangeListeners;
		this.valueDependencies = valueDependencies;
		if (valueDependencies == null) {
			CalculateValueDependencies ();
		}
	}

	public TightBucketsOfUniques<string, string> GetDependencies () {
		return valueDependencies;
	}

	void CalculateValueDependency(string valueName) {
		watchingDependency = valueName;
		dependencyDepth = 0;
		Get (valueName);
		watchingDependency = null;
		dependencyDepth = -1;
	}

	public TightBucketsOfUniques<string, string> CalculateValueDependencies() {
		valueDependencies = new TightBucketsOfUniques<string, string> ();
		foreach (var kvp in valueCalculationRules) {
			CalculateValueDependency (kvp.Key);
		}
//		string dependencyChart = "";
//		foreach(var d in valueDependencies) {
//			string str = "";
//			if(d.Value != null) {
//				for(int i=0;i<d.Value.Length;++i){
//					if(i>0)str+=", ";
//					str+=d.Value[i];
//				}
//			}
//			dependencyChart += ((dependencyChart.Length > 0)?"\n":"") + d.Key + ": " + str;
//		}
//		Debug.Log ("dependencies for "+target+": ["+dependencyChart+"]");
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

	public string DEBUG_GetDependenciesBeingCalculated() {
		string str = "";
		foreach (string s in dependenciesBeingCalculated) {
			str += ((str.Length > 0) ? ", " : "") + s;
		}
		return "[" + str + "]";
	}

	public string DEBUG_GetWhatNeedsCalculation(){
		string str = "";
		foreach (string s in needsCalculation) {
			str += ((str.Length > 0) ? ", " : "") + s;
		}
		return "[" + str + "]";
	}

	public float Calculate(string valueName, float value) {
		#if PREVENT_STACK_OVERFLOW
		if(dependenciesBeingCalculated.Contains(valueName)) {
			throw new UnityException("dependency stack recursion: "+DEBUG_GetDependenciesBeingCalculated()+" "+valueName);
		}
		dependenciesBeingCalculated.Add(valueName);
		#endif
//		if (valueDependencies != null && watchingDependency != null && watchingDependency != valueName) {
//			valueDependencies.AddUniqueBucketItem (valueName, watchingDependency);
//		}
		if (modifiers != null) {
			modifiers.ForEachUniqueInBucket (valueName, (m) => {
				value = m.Modify (value);
			});
		}
		#if PREVENT_STACK_OVERFLOW
		if(dependenciesBeingCalculated[dependenciesBeingCalculated.Count-1] != valueName) {
			throw new UnityException("dependency stack ejection problem: "+dependenciesBeingCalculated+" "+valueName);
		}
		dependenciesBeingCalculated.RemoveAt(dependenciesBeingCalculated.Count-1);
		#endif
//		if (valueDependencies != null) {
//			// invalidate values that depend on this one, which will cause them to re-calculate next time they are checked.
//			valueDependencies.ForEachUniqueInBucket (valueName, (dependency) => { InvalidateCache (dependency); });
//		}
		return value;
	}

	/// <summary>if there are invalid values that need to be recalculated, recalculate those, and execute value listeners</summary>
	public void DoCalculations() {
//		float value;
//		ChangeListener[] listeners = null;
//		// find which elements are calculated
//		foreach (var calc in valueCalculationRules) {
//			// and have change listeners
//			if (changeListeners.TryGetValue (calc.Key, out listeners) && listeners != null && listeners.Length > 0
//				// if these are invalid
//				&& !values.TryGetValue(calc.Key, out value)) {
//				// then get the new value, and execute those listeners
//				value = Get (calc.Key);
//				changeListeners.ForEachUniqueInBucket (calc.Key, listener => listener (target, calc.Key, 0, value));
//			}
//		}

//		float value, oldValue;
		//foreach (string s in needsCalculation) {
		string[] arr= new string[40];
		while(needsCalculation.Count > 0) {
			needsCalculation.CopyTo(arr);
			string dependency = arr [0];
			ValueCalculation<float> initFunc;
			if (valueCalculationRules.TryGetValue (dependency, out initFunc)) {
				Set(dependency, Calculate (dependency, initFunc (target)));
			}
			needsCalculation.Remove (dependency);
		}
	}

	public void InvalidateCache(string valueName) {
		needsCalculation.Add (valueName);
		//values.Remove (valueName);
	}

	public void InvalidateAllCalculatedValues() {
		foreach(var kvp in valueCalculationRules) {
			if (kvp.Value != null) {
				needsCalculation.Add (kvp.Key);
				//values.Remove (kvp.Key);
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
			if (dependencyDepth == 1) {
				valueDependencies.AddUniqueBucketItem (valueName, watchingDependency);
			}
		}
		float result;
		if (!values.TryGetValue (valueName, out result) || watchingDependency != null || needsCalculation.Contains(valueName)) {
			ValueCalculation<float> initFunc;
			if (valueCalculationRules.TryGetValue (valueName, out initFunc)) {
				if (dependencyDepth >= 0) { dependencyDepth++; }
				result = Calculate (valueName, initFunc (target));
//				values [valueName] = result;
//				invalids.Remove(valueName);
				Set(valueName, result);
				if (dependencyDepth >= 0) { dependencyDepth--; }
			}
		}
		return result;
	}

	public void Set(string valueName, float newValue) {
//		#if FAIL_ON_CALCULATED_VALUE_ASSIGNMENT
//		// if this value is calculated, fail.
//		ValueCalculation<float> initFunc;
//		if (valueCalculationRules.TryGetValue (valueName, out initFunc)) {
//			throw new UnityException ("Cannot assign to automatically-calculated value \'"+valueName+"\'");
//		}
//		#endif
		if (changeListeners != null || staticChangeListeners != null) {		// if there are change listeners
			float oldValue = 0;
			if (!values.TryGetValue (valueName, out oldValue)) {
				oldValue = 0;
			}
			values [valueName] = newValue;
			needsCalculation.Remove (valueName);
			if (changeListeners != null) {
				changeListeners.ForEachUniqueInBucket (valueName, thing => thing (target, valueName, oldValue, newValue));
				changeListeners.ForEachUniqueInBucket ("", thing => thing (target, valueName, oldValue, newValue));
			}
			if (staticChangeListeners != null) {
				ChangeListener listener;
				if (staticChangeListeners.TryGetValue (valueName, out listener)) {
					listener (target, valueName, oldValue, newValue);
				}
			}
		} else {
			values [valueName] = newValue;
			needsCalculation.Remove(valueName);
		}
		if (valueDependencies != null && watchingDependency == null) { // if other values might depend on this one
//			string str = "";
			// invalidate values that depend on this one, which will cause them to re-calculate next time they are checked.
			valueDependencies.ForEachUniqueInBucket (valueName, (dependency) => { 
//				str += ((str.Length > 0)?", ":"")+dependency;

				ValueCalculation<float> initFunc;
				if (valueCalculationRules.TryGetValue (dependency, out initFunc)) {
					Set(dependency, Calculate (dependency, initFunc (target)));
				}

//				InvalidateCache (dependency); 
			});
			// don't update depending members if we're calculating dependency
			if (watchingDependency == null) {
				//Debug.Log(valueName+" invalidated ["+str+"] for "+target.ToString()+"\ninvalid: "+DEBUG_GetWhatNeedsCalculation());
				DoCalculations ();
			}
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
