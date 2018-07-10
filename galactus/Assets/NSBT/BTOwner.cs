using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BTOwner : MonoBehaviour {
	/// <summary>The behavior</summary>
	public BT.Behavable behavior;

	/// <summary>how long in milliseconds this has been updating</summary>
	private long uptime = 0;
	/// <summary>keep track of fractional milliseconds</summary>
	private float fractionalMS;
	public long GetUpTimeMS(){ return uptime; }

	/// <summary> when to update behavior tree logic</summary>
	private long whenToUpdate;
	/// <summary>how many times the behavior tree updated</summary>
	private int iterations = 0;

	/// <summary>Millisecond timer. an int because Unity3D seems to have trouble with longs in the inspector.</summary>
	[Tooltip("Time between AI ticks, in milliseconds")]
	public int aiTimerMS = 100;

	[Tooltip("If null, will seek a BTBehavior in this object")]
	public BehaviorTree scriptSource;

	/// The behavior stack of behaviors to execute
	public Stack<BT.Behavior> behaviorStack = new Stack<BT.Behavior>();
	
	[HideInInspector]
	public IDictionary<object,object> variables = new Dictionary<object,object>();

	/// <summary>
	/// Used to generalize any object with a position into a location
	/// </summary>
	/// <returns>The location.</returns>
	/// <param name="named">Named.</param>
	public Spatial.Locatable GetLocation(string named) {
		object o = variables[named];
		if(o is Spatial.Locatable)
			return (Spatial.Locatable)o;
		else if(o is Vector3)
			return new Spatial.Location((Vector3)o);
		else if(o is Transform)
			return new Spatial.Location((Transform)o);
		else if(o is GameObject)
			return new Spatial.Location(((GameObject)o).transform);
		else if(o is MonoBehaviour)
			return new Spatial.Location(((MonoBehaviour)o).gameObject.transform);
		return null;
	}

	// Use this for initialization
	void Start () {
		if(scriptSource != null) {
			behavior = scriptSource.GetComponent<BehaviorTree>();
		}
	}

	// Update is called once per frame
	void Update () {
		// update uptime timer
		float msPassed = Time.deltaTime * 1000;
		int iMsPassed = (int)msPassed;
		fractionalMS += msPassed - iMsPassed;
		uptime += iMsPassed;
		if(fractionalMS > 1) {
			uptime += (int)fractionalMS;
			fractionalMS -= 1;
		}
		//whenToUpdate += Time.deltaTime;
		if(uptime > whenToUpdate) {
			BT.Behavable whatToDo = behavior;
			if(behaviorStack.Count != 0) {
				whatToDo = behaviorStack.Peek();
			}
			whatToDo.Behave(this);
			whenToUpdate = whenToUpdate + aiTimerMS;
			iterations++;
		}
	}

	public BT.Behavior GetCurrentBehavior() {
		if(behaviorStack.Count == 0)
			return null;
		return behaviorStack.Peek ();
	}
	/// <summary>accessible by scripting system</summary>
	/// <returns>The global game object.</returns>
	/// <param name="name">Name.</param>
	public GameObject FindGlobalGameObject(string name) {
		return GameObject.Find (name);
	}
}
