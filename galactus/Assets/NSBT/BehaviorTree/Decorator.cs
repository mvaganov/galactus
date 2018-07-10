using UnityEngine;
using System.Collections;

namespace BT {
	/// <summary>a middleman between tasks. useful for switching, altering, or adding conditions to the output of another task</summary>
	public class Decorator : Behavior {

		public Behavable child;

		override public Status Execute(BTOwner who) {
			return child.Behave(who);
		}

		public void SetChild(Behavior child) {
			this.child = child;
		}
	}
}