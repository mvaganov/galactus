using UnityEngine;
using System.Collections;

namespace BT {
	public class WithinRange : Behavior {
		public string var;
		public float range;

		override public Status Execute (BTOwner whoExecutes) {
			object o;
			bool found = whoExecutes.variables.TryGetValue (var, out o);
			Status state = Status.failure;
			if(found && o is Vector3) {
				Vector3 loc = whoExecutes.GetLocation(var).GetLocation();
				Vector3 delta = loc - whoExecutes.transform.position;
	//			Debug.Log("LOC: "+loc+"   DELTA: "+delta+"  dist: "+delta.magnitude);
				if(delta.magnitude < range) {
	//				Debug.Log("CLOSE ENOUGH! "+delta.magnitude+" < "+range);
					state = Status.success;
				}
			}
	//		if(state == Status.failure)
	//			Debug.Log("Too far!");
			return state;
		}
		//override public bool HasState(){return false;}
	}
}