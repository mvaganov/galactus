using UnityEngine;
using System.Collections;

namespace BT {
	public class SetVariableToResource : Behavior {
		public string name;
		public string objectName;
		
		override public Status Execute (BTOwner who) {
			Object o = Resources.Load(objectName);
//Debug.Log ("found \"" + o + "\"");
			who.variables.Add (name, o);
			return Status.success;
		}
		// override public bool HasState(){return false;}
	}
}