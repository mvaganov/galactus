using UnityEngine;
using System.Collections;

namespace BT {
	public class ClearVariable : Behavior {
		public string var;

		override public Status Execute (BTOwner whoExecutes) {
			object o;
			bool found = whoExecutes.variables.TryGetValue (var, out o);
			if (found) {
				whoExecutes.variables.Remove(var);
			}
			return Status.success;
		}
		// override public bool HasState(){return false;}
	}
}