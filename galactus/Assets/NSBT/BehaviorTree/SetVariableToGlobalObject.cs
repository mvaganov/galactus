using UnityEngine;
using System.Collections;

namespace BT {
	public class SetVariableToGlobalObject : Behavior {
		public string nameOfVariable;
		public string objectName;

		override public Status Execute (BTOwner whoExecutes) {
			GameObject go = GameObject.Find (objectName);
//Debug.Log ("found "+objectName+" as \"" + go + "\", AKA "+objectName);
			whoExecutes.variables.Add (nameOfVariable, go);
			return Status.success;
		}
		//override public bool HasState(){return false;}
	}
}