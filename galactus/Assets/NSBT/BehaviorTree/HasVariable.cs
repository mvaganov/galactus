using UnityEngine;
using System.Collections;

namespace BT {
	public class HasVariable : Behavior {

		public string name;

		override public Status Execute (BTOwner who) {
			object obj;
			bool found = (OMU.Data.TryDeReferenceGet(who.variables, name, out obj));
			return found?Status.success:Status.failure;
		}
		// override public bool HasState(){return false;}
	}
}