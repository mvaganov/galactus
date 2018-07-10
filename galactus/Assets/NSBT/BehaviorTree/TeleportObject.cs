using UnityEngine;
using System.Collections;

namespace BT {
	public class TeleportObject : Behavior {

		public string targetName;
		public string destinationName;

		override public Status Execute(BTOwner who) {
			object obj;// = who.variables.GetProperty(targetName);
			OMU.Data.TryDeReferenceGet(who.variables, targetName, out obj);
			GameObject target = obj as GameObject;
			target.transform.position = who.GetLocation(destinationName).
				GetClosestPointTo(target.transform.position);
			return Status.success;
		}
		//override public bool HasState(){return false;}
	}
}