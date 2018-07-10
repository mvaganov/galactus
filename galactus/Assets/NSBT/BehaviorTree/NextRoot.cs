using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>changes the root of the behavior tree for this BTOwner to the child node</summary>
	public class NextRoot : Decorator {
		override public Status Execute(BTOwner who) {
			if(child == null)
				throw new System.Exception("cannot set main script to null value.");
			// clear everything but this from the stack
			who.behaviorStack.Clear ();
			who.behavior = child;
			return Status.success;
		}
		//override public bool HasState(){return false;}
	}
}